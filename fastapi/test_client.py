#!/usr/bin/env python3
"""
Script de teste para o servidor OCR FastAPI
Uso: python test_client.py <endpoint> <arquivo>
Exemplo: python test_client.py ocr imagem.png
Exemplo: python test_client.py transcribe audio.wav
"""

import sys
import requests
import json
from pathlib import Path

BASE_URL = "http://localhost:8000"


def test_ocr(image_path: str, lang: str = "pt", debug: bool = False):
    """Testa o endpoint /ocr"""
    url = f"{BASE_URL}/ocr"
    
    if not Path(image_path).exists():
        print(f"Erro: Arquivo não encontrado: {image_path}")
        return
    
    print(f"Enviando imagem: {image_path}")
    print(f"URL: {url}")
    print(f"Parâmetros: lang={lang}, debug={debug}")
    print("-" * 50)
    
    try:
        with open(image_path, "rb") as f:
            files = {"file": (Path(image_path).name, f, "image/png")}
            data = {"lang": lang, "debug": str(debug).lower()}
            response = requests.post(url, files=files, data=data, timeout=60)
        
        if response.status_code == 200:
            result = response.json()
            print("✅ Sucesso!")
            print("\nResultado:")
            print(json.dumps(result, indent=2, ensure_ascii=False))
            
            if "rawText" in result:
                print(f"\n📝 Texto extraído:\n{result['rawText']}")
        else:
            print(f"❌ Erro {response.status_code}: {response.text}")
            
    except requests.exceptions.ConnectionError:
        print("❌ Erro: Não foi possível conectar ao servidor.")
        print("   Verifique se o container está rodando:")
        print("   docker-compose -f ../docker/docker-compose.keycloak.yml ps ocr-api")
    except Exception as e:
        print(f"❌ Erro: {e}")


def test_transcribe(audio_path: str, language: str = "pt"):
    """Testa o endpoint /transcribe"""
    url = f"{BASE_URL}/transcribe"
    
    if not Path(audio_path).exists():
        print(f"Erro: Arquivo não encontrado: {audio_path}")
        return
    
    print(f"Enviando áudio: {audio_path}")
    print(f"URL: {url}")
    print(f"Parâmetros: language={language}")
    print("-" * 50)
    
    try:
        with open(audio_path, "rb") as f:
            files = {"file": (Path(audio_path).name, f, "audio/wav")}
            data = {"language": language}
            response = requests.post(url, files=files, data=data, timeout=120)
        
        if response.status_code == 200:
            result = response.json()
            print("✅ Sucesso!")
            print("\nResultado:")
            print(json.dumps(result, indent=2, ensure_ascii=False))
            
            if "text" in result:
                print(f"\n📝 Texto transcrito:\n{result['text']}")
        else:
            print(f"❌ Erro {response.status_code}: {response.text}")
            
    except requests.exceptions.ConnectionError:
        print("❌ Erro: Não foi possível conectar ao servidor.")
        print("   Verifique se o container está rodando:")
        print("   docker-compose -f ../docker/docker-compose.keycloak.yml ps ocr-api")
    except Exception as e:
        print(f"❌ Erro: {e}")


def main():
    if len(sys.argv) < 3:
        print("Uso: python test_client.py <endpoint> <arquivo> [opções]")
        print("\nEndpoints disponíveis:")
        print("  ocr        - OCR de imagens")
        print("  transcribe - Transcrição de áudio")
        print("\nExemplos:")
        print("  python test_client.py ocr imagem.png")
        print("  python test_client.py ocr imagem.png --debug")
        print("  python test_client.py transcribe audio.wav")
        sys.exit(1)
    
    endpoint = sys.argv[1].lower()
    file_path = sys.argv[2]
    
    if endpoint == "ocr":
        debug = "--debug" in sys.argv
        lang = "pt"
        if "--lang" in sys.argv:
            idx = sys.argv.index("--lang")
            if idx + 1 < len(sys.argv):
                lang = sys.argv[idx + 1]
        test_ocr(file_path, lang=lang, debug=debug)
    elif endpoint == "transcribe":
        language = "pt"
        if "--language" in sys.argv:
            idx = sys.argv.index("--language")
            if idx + 1 < len(sys.argv):
                language = sys.argv[idx + 1]
        test_transcribe(file_path, language=language)
    else:
        print(f"❌ Endpoint desconhecido: {endpoint}")
        print("Endpoints disponíveis: ocr, transcribe")
        sys.exit(1)


if __name__ == "__main__":
    main()


