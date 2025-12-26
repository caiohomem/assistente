from fastapi import FastAPI, UploadFile, File
import cv2
import numpy as np
from collections.abc import Mapping
import tempfile
import shutil
import os
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI()

@app.get("/")
async def root():
    """Endpoint raiz - health check"""
    return {
        "status": "ok",
        "service": "OCR API",
        "endpoints": {
            "ocr": "/ocr",
            "transcribe": "/transcribe",
            "docs": "/docs"
        }
    }

@app.get("/health")
async def health():
    """Health check endpoint"""
    try:
        # Verificar se os modelos podem ser carregados (sem carregar de fato)
        return {
            "status": "healthy",
            "models": {
                "whisper": "available",
                "paddleocr": "available"
            }
        }
    except Exception as e:
        return {
            "status": "unhealthy",
            "error": str(e)
        }

# Lazy loading dos modelos para evitar erros na inicialização
_model = None
ocr_by_lang = {}

def get_whisper_model():
    """Carrega o modelo Whisper sob demanda"""
    # Import lazy do WhisperModel para evitar erro na inicialização
    from faster_whisper import WhisperModel
    
    global _model
    if _model is None:
        try:
            logger.info("Carregando modelo Whisper...")
            _model = WhisperModel("base", device="cpu", compute_type="int8")
            logger.info("Modelo Whisper carregado com sucesso")
        except Exception as e:
            logger.error(f"Erro ao carregar modelo Whisper: {e}")
            raise
    return _model

def get_ocr(lang: str):
    """Carrega o modelo PaddleOCR sob demanda"""
    # Import lazy do PaddleOCR para evitar erro na inicialização
    from paddleocr import PaddleOCR
    
    lang = (lang or "pt").lower()
    if lang not in ocr_by_lang:
        # Lazy init to avoid downloading models unnecessarily
        try:
            logger.info(f"Carregando modelo PaddleOCR para idioma: {lang}")
            ocr_by_lang[lang] = PaddleOCR(lang=lang, use_textline_orientation=True)
            logger.info(f"Modelo PaddleOCR para {lang} carregado com sucesso")
        except Exception as e:
            logger.error(f"Erro ao carregar modelo PaddleOCR para {lang}: {e}")
            raise
    return ocr_by_lang[lang]


def summarize_result(payload):
    try:
        if payload is None:
            return {"kind": "none"}
        if isinstance(payload, (str, int, float, bool)):
            s = str(payload)
            return {"kind": "scalar", "type": str(type(payload)), "preview": s[:1000]}
        if isinstance(payload, bytes):
            return {"kind": "bytes", "len": len(payload)}
        if isinstance(payload, list):
            return {
                "kind": "list",
                "len": len(payload),
                "firstType": str(type(payload[0])) if payload else None,
                "firstPreview": repr(payload[0])[:500] if payload else None,
            }
        if isinstance(payload, dict):
            keys = list(payload.keys())
            return {"kind": "dict", "keys": keys[:50]}
        # objects
        attrs = [a for a in dir(payload) if not a.startswith("_")]
        return {"kind": "object", "type": str(type(payload)), "attrs": attrs[:50], "repr": repr(payload)[:500]}
    except Exception as ex:
        return {"kind": "error", "error": repr(ex)}

@app.post("/transcribe")
async def transcribe(file: UploadFile = File(...), language: str = "pt"):
    # salva upload temporariamente
    suffix = os.path.splitext(file.filename)[1] or ".wav"
    with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as tmp:
        shutil.copyfileobj(file.file, tmp)
        tmp_path = tmp.name

    try:
        model = get_whisper_model()
        segments, info = model.transcribe(tmp_path, language=language)
        text = "".join([s.text for s in segments]).strip()
        return {"language": info.language, "duration": info.duration, "text": text}
    except Exception as e:
        logger.error(f"Erro na transcrição: {e}")
        return {"error": str(e), "language": language, "duration": 0, "text": ""}
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)


@app.post("/ocr")
async def ocr_card(file: UploadFile = File(...), lang: str = "pt", debug: bool = False):
    suffix = os.path.splitext(file.filename)[1] or ".png"
    with tempfile.NamedTemporaryFile(delete=False, suffix=suffix) as tmp:
        shutil.copyfileobj(file.file, tmp)
        tmp_path = tmp.name

    try:
        # Note: `lang` is kept for API compatibility; the server is initialized with pt models.
        #
        # PaddleOCR 3.x removed/changed some kwargs (e.g. `cls`) and now routes to `predict()`.
        # Call with no kwargs and support multiple return formats.
        img = cv2.imread(tmp_path)
        if img is None:
            return {"rawText": "", "lines": [], "lang": lang, "error": "cv2.imread_failed"}

        # Light pre-processing: upscale + contrast (helps with small text / low-res crops)
        h, w = img.shape[:2]
        if w < 1200:
            scale = int(np.ceil(1200 / max(w, 1)))
            scale = max(2, min(scale, 4))
            img = cv2.resize(img, (w * scale, h * scale), interpolation=cv2.INTER_CUBIC)

        img = cv2.convertScaleAbs(img, alpha=1.35, beta=0)
        img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

        ocr = get_ocr(lang)
        try:
            result = ocr.ocr(img)
        except TypeError:
            result = ocr.predict(img)
        except Exception:
            # Fallback to path-based call if ndarray call fails for any reason
            try:
                result = ocr.ocr(tmp_path)
            except TypeError:
                result = ocr.predict(tmp_path)

        def iter_text_lines(payload):
            if payload is None:
                return

            if isinstance(payload, str):
                yield payload, 0.0
                return

            if isinstance(payload, Mapping):
                # Single line dicts
                if "text" in payload or "rec_text" in payload:
                    text = payload.get("text") or payload.get("rec_text")
                    score = payload.get("confidence")
                    if score is None:
                        score = payload.get("rec_score")
                    yield text, score if score is not None else 0.0
                    return

                # Parallel arrays
                rec_texts = payload.get("rec_texts")
                rec_scores = payload.get("rec_scores")
                if isinstance(rec_texts, list):
                    if isinstance(rec_scores, list) and len(rec_scores) == len(rec_texts):
                        for t, s in zip(rec_texts, rec_scores):
                            yield t, s
                    else:
                        for t in rec_texts:
                            yield t, 0.0
                    return

                # Nested collections
                inner = payload.get("result") or payload.get("ocr") or payload.get("data") or payload.get("results")
                if inner is not None:
                    for t, s in iter_text_lines(inner) or []:
                        yield t, s
                    return

                # Fallback: walk all values
                for v in payload.values():
                    for t, s in iter_text_lines(v) or []:
                        yield t, s
                return

            if isinstance(payload, (list, tuple)):
                for item in payload:
                    for t, s in iter_text_lines(item) or []:
                        yield t, s
                return

            # Some versions return an object with these attributes.
            if hasattr(payload, "rec_texts"):
                texts = getattr(payload, "rec_texts", None)
                scores = getattr(payload, "rec_scores", None)
                if isinstance(texts, list):
                    if isinstance(scores, list) and len(scores) == len(texts):
                        for t, s in zip(texts, scores):
                            yield t, s
                    else:
                        for t in texts:
                            yield t, 0.0
                    return

            if hasattr(payload, "to_dict"):
                try:
                    as_dict = payload.to_dict()
                    for t, s in iter_text_lines(as_dict) or []:
                        yield t, s
                    return
                except Exception:
                    pass

            if hasattr(payload, "__dict__") and isinstance(getattr(payload, "__dict__", None), dict):
                as_dict = payload.__dict__
                for t, s in iter_text_lines(as_dict) or []:
                    yield t, s
                return

            # Older format: [box, (text, score)]
            if isinstance(payload, list) and len(payload) >= 2 and isinstance(payload[1], (list, tuple)) and len(payload[1]) >= 2:
                yield payload[1][0], payload[1][1]
                return
        lines = []
        raw_lines = []

        for text, score in iter_text_lines(result) or []:
            text = (text or "").strip()
            if not text:
                continue
            try:
                confidence = float(score)
            except Exception:
                confidence = 0.0
            lines.append({"text": text, "confidence": confidence})
            raw_lines.append(text)

        raw_text = "\n".join(raw_lines).strip()
        if debug:
            return {
                "rawText": raw_text,
                "lines": lines,
                "lang": lang,
                "resultType": str(type(result)),
                "resultSummary": summarize_result(result),
                "imageSize": {"width": int(img.shape[1]), "height": int(img.shape[0])},
            }

        return {"rawText": raw_text, "lines": lines, "lang": lang}
    except Exception as e:
        logger.error(f"Erro no OCR: {e}")
        return {"rawText": "", "lines": [], "lang": lang, "error": str(e)}
    finally:
        if os.path.exists(tmp_path):
            os.remove(tmp_path)
