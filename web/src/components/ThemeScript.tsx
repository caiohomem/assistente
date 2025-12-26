export function ThemeScript() {
  // Este script deve ser executado de forma síncrona antes de qualquer renderização
  // para evitar flash de conteúdo incorreto (FOUC - Flash of Unstyled Content)
  return (
    <script
      id="theme-init"
      dangerouslySetInnerHTML={{
        __html: `
          (function() {
            try {
              // Lógica sincronizada com ThemeProvider
              const savedTheme = localStorage.getItem('theme');
              const theme = (savedTheme && ['light', 'dark', 'system'].includes(savedTheme)) 
                ? savedTheme 
                : 'system';
              
              const root = document.documentElement;
              
              // Calcular tema efetivo ANTES de modificar o DOM
              let effectiveTheme;
              if (theme === 'system') {
                effectiveTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
              } else {
                effectiveTheme = theme;
              }
              
              // REMOVER TODAS as classes de tema primeiro (garantir estado limpo)
              // Usar múltiplas chamadas para garantir remoção completa
              root.classList.remove('light');
              root.classList.remove('dark');
              
              // Tailwind CSS v4: aplicar classe 'dark' APENAS se o tema efetivo for 'dark'
              // Para tema claro, NÃO adicionar a classe 'dark' (padrão do Tailwind)
              if (effectiveTheme === 'dark') {
                root.classList.add('dark');
              } else {
                // CRÍTICO: Garantir explicitamente que a classe 'dark' seja removida para tema claro
                // Remover múltiplas vezes para garantir que não há classe residual
                root.classList.remove('dark');
                // Verificar novamente e remover se ainda estiver presente
                if (root.classList.contains('dark')) {
                  root.classList.remove('dark');
                }
              }
              
              // Atualizar atributo data-theme para referência
              root.setAttribute('data-theme', effectiveTheme);
              
              // Forçar reflow para garantir que o navegador aplique as mudanças imediatamente
              void root.offsetHeight;
              
              // Verificação final: garantir que o estado está correto
              if (effectiveTheme === 'light' && root.classList.contains('dark')) {
                root.classList.remove('dark');
              }
              
              // Observer para garantir que a classe 'dark' seja removida se algo tentar adicioná-la
              // quando o tema é 'light' - executar por 10 segundos após carregamento
              if (effectiveTheme === 'light') {
                let checkCount = 0;
                const maxChecks = 20; // Verificar 20 vezes (10 segundos)
                
                const checkAndRemoveDark = function() {
                  const currentTheme = localStorage.getItem('theme');
                  let shouldBeLight = false;
                  
                  if (!currentTheme || currentTheme === 'light') {
                    shouldBeLight = true;
                  } else if (currentTheme === 'system') {
                    shouldBeLight = !window.matchMedia('(prefers-color-scheme: dark)').matches;
                  }
                  
                  if (shouldBeLight && root.classList.contains('dark')) {
                    root.classList.remove('dark');
                  }
                  
                  checkCount++;
                  if (checkCount < maxChecks) {
                    setTimeout(checkAndRemoveDark, 500);
                  }
                };
                
                // Iniciar verificações após um pequeno delay
                setTimeout(checkAndRemoveDark, 100);
              }
            } catch (e) {
              // Fallback: remover classes e deixar padrão (light)
              const root = document.documentElement;
              root.classList.remove('light');
              root.classList.remove('dark');
              root.setAttribute('data-theme', 'light');
            }
          })();
        `,
      }}
    />
  )
}

