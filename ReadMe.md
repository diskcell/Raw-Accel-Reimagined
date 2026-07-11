<p align="center">
  <img src="modern-ui/branding/raw-accel-reimagined-logo.png" width="150" alt="Raw Accel Reimagined logo">
</p>

# Raw Accel Reimagined

Uma interface moderna, bilíngue e orientada a perfis para o driver Raw Accel. O projeto preserva o motor de aceleração original e concentra as mudanças na experiência de configuração, visualização e gerenciamento.

Modern, bilingual and profile-oriented UI for the Raw Accel driver. The original acceleration engine is preserved; this project focuses on configuration, visualization and management.

## Principais recursos / Highlights

- Interface WPF moderna com tema escuro e gráfico interativo em tempo real.
- Português do Brasil e inglês em toda a interface.
- Perfis criáveis e duplicáveis, com associação a dispositivos detectados.
- Ajustes de eixos, resposta vertical e suavização com ajuda contextual.
- Minimização para a bandeja do Windows e desligamento explícito do driver.
- Modern WPF UI, live graph, device-aware profiles, axis tuning, contextual help and system tray support.

## Executar / Run

1. Faça uma cópia de `settings.example.json` com o nome `settings.json` na raiz.
2. Instale o driver com `installer.exe` se ele ainda não estiver instalado.
3. Abra `RawAccelReimagined.exe`.

Seu `settings.json`, preferências locais e IDs de dispositivos não são versionados. Isso impede que perfis pessoais sejam publicados acidentalmente.

Copy `settings.example.json` to `settings.json`, install the driver if needed, then run `RawAccelReimagined.exe`. Local settings and hardware IDs are intentionally excluded from Git.

## Compilar / Build

Requisitos: Windows x64, .NET Framework 4.7.2 Developer Pack e MSBuild.

```powershell
MSBuild.exe modern-ui\RawAccelModern.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

A saída é criada em `modern-ui\bin\Release\RawAccelReimagined.exe`.

## Base e créditos / Upstream and credits

Este projeto é baseado no [Raw Accel original](https://github.com/a1xd/rawaccel), distribuído sob licença MIT. O driver e as fórmulas de aceleração pertencem ao trabalho original; a licença e os créditos foram preservados.

Original Raw Accel contributors: simon (driver and acceleration logic), \_m00se\_ (GUI, gain features and acceleration types), Sidiouth, TauntyArmordillo and Kovaak.

Consulte também o [guia original](doc/Guide.md) e a [FAQ](doc/FAQ.md).
