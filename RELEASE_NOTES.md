# Raw Accel Reimagined 1.8.3

## English

This release delivers a major memory-efficiency improvement and a complete bilingual Help Center inside the application.

### Major memory improvements

- Switched the mostly static WPF interface to a lighter software-rendering path, avoiding expensive graphics allocations without changing mouse calculations.
- In local isolated measurements, private memory dropped from approximately **182 MB to 99 MB** — a reduction of about **46%**.
- Visible resident memory dropped from approximately **123 MB to 113 MB** in the same environment.
- When hidden in the system tray, resident memory is now actively released and measured approximately **2–18 MB** locally instead of about **111 MB**.
- Repeated navigation and guide use remained stable, with no application memory leak detected.
- Idle CPU impact from the lighter renderer was negligible in testing.

Memory values vary by Windows version, GPU driver, theme, display resolution and active pages. The acceleration formulas, Raw Accel driver communication and saved-profile behavior were not changed.

### New built-in Help Center

- Added a complete Help page in English and Brazilian Portuguese.
- Added a four-step Getting Started guide, explanations for Natural, Classic and LUT curves, common questions, safety tips and live application status.
- Added quick navigation to Charts and Driver & Recovery.
- The **Complete Guide** and **FAQ** now open in a themed, scrollable in-app viewer.
- Documentation no longer launches VS Code or another external Markdown editor.
- Added full built-in Guide and FAQ content in both supported languages.

User profiles, settings, themes, language preferences and mouse associations remain preserved during automatic updates.

## Português

Esta versão traz uma grande melhoria no uso de memória e uma Central de Ajuda completa e bilíngue dentro do aplicativo.

### Grandes melhorias de memória

- A interface WPF, que é majoritariamente estática, passou a usar uma renderização por software mais leve, evitando alocações gráficas caras sem alterar os cálculos do mouse.
- Nas medições locais isoladas, a memória privada caiu de aproximadamente **182 MB para 99 MB** — uma redução de cerca de **46%**.
- A memória residente com a janela visível caiu de aproximadamente **123 MB para 113 MB** no mesmo ambiente.
- Quando escondido na bandeja, o aplicativo agora libera a memória residente, que mediu aproximadamente **2–18 MB** localmente em vez de cerca de **111 MB**.
- A memória permaneceu estável durante navegação e uso repetido do guia, sem vazamento de memória detectado no aplicativo.
- O impacto da renderização mais leve sobre a CPU ociosa foi insignificante nos testes.

Os valores de memória variam conforme a versão do Windows, driver da GPU, tema, resolução da tela e páginas utilizadas. As fórmulas de aceleração, a comunicação com o driver Raw Accel e o comportamento dos perfis salvos não foram alterados.

### Nova Central de Ajuda integrada

- Adicionada uma página de Ajuda completa em inglês e português brasileiro.
- Adicionados primeiros passos em quatro etapas, explicações das curvas Natural, Clássica e LUT, dúvidas comuns, dicas de segurança e status do aplicativo.
- Adicionada navegação rápida para Gráficos e Driver e recuperação.
- O **Guia completo** e o **FAQ** agora abrem em um visualizador interno tematizado e com rolagem.
- A documentação não abre mais o VS Code ou outro editor Markdown externo.
- Adicionado conteúdo completo do Guia e FAQ nos dois idiomas disponíveis.

Perfis, configurações, temas, idioma e associações de mouse continuam preservados durante atualizações automáticas.
