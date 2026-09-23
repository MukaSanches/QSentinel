# QSentinel

> Intelligent adaptive Windows resource orchestrator with a safety-first architecture and a hybrid classical/quantum research layer.

[![Windows Build](https://github.com/MukaSanches/QSentinel/actions/workflows/windows-build.yml/badge.svg)](https://github.com/MukaSanches/QSentinel/actions/workflows/windows-build.yml)
![Version](https://img.shields.io/badge/version-1.1.0-6f67ff)
![Platform](https://img.shields.io/badge/platform-Windows%20x64-0078d4)
![.NET](https://img.shields.io/badge/.NET-8.0-512bd4)

**Languages:** [English](#english) · [Português](#português) · [Español](#español)

---

# English

## Overview

**QSentinel** is a Windows resource orchestration application designed to monitor the system continuously and reduce unnecessary background pressure on CPU, memory, and disk I/O without blindly terminating applications.

The project combines:

- real-time Windows process monitoring;
- adaptive system-pressure analysis;
- foreground-application awareness;
- reversible process optimization;
- protected-process safety rules;
- startup integration and tray residency;
- manual application blocking;
- live performance charts;
- classical optimization research;
- hybrid quantum/classical experiments using qBraid.

The Windows application is built with **C# / .NET 8 / Windows Forms**. The research layer uses **Python**.

## What QSentinel 1.1 does

### Real-time monitoring

QSentinel continuously measures:

- total CPU utilization;
- physical memory pressure;
- aggregate process I/O activity;
- per-process CPU usage;
- per-process memory usage;
- per-process I/O activity;
- the application currently in the foreground.

The default monitoring interval is approximately **1.5 seconds**.

### Adaptive optimization engine

QSentinel calculates a system-pressure score and reacts according to current load.

When pressure rises, it can select safe background processes and apply reversible controls such as:

- **Below Normal** process priority;
- Windows execution-speed power throttling / Eco-style behavior;
- reduced memory priority under high memory pressure.

When the pressure drops, QSentinel restores the previous process priority.

QSentinel does **not** treat high memory usage alone as a reason to terminate software.

### Safety Engine

The Safety Engine prevents automatic optimization of protected Windows processes, security components, QSentinel itself, and the current foreground application.

The architecture is intentionally:

```text
Monitoring
   ↓
Adaptive decision engine
   ↓
Safety Engine
   ↓
Reversible Windows intervention
```

Not:

```text
Optimizer → unrestricted process termination
```

### Windows startup integration

QSentinel can register itself to start automatically with Windows.

When launched at login, it starts in background mode and remains available through the Windows notification area.

Closing the main window sends the application to the tray instead of stopping the engine.

The user or a Windows administrator can still stop or uninstall QSentinel.

### Application blocking

A user can explicitly add a non-protected background process to the block list.

Only applications intentionally added to that list are automatically terminated when detected.

Unknown applications are **not** killed automatically.

### Live dashboard

QSentinel 1.1 includes a redesigned dashboard with:

- CPU metric;
- memory metric;
- disk / I/O metric;
- number of currently optimized processes;
- system-pressure indicator;
- CPU history chart;
- memory history chart;
- I/O activity chart;
- process table with CPU, memory, I/O, PID and status;
- visible states such as **PROTECTED**, **IN USE**, **HIGH IMPACT**, **WATCH**, and **OPTIMIZED**.

## Technology

### Windows application

- **C#**
- **.NET 8**
- **Windows Forms**
- Windows native APIs through P/Invoke
- Windows Registry startup integration
- Windows process priority management
- Windows power throttling / execution-speed control
- process memory-priority management
- foreground-window detection
- process I/O counters
- notification-area integration

### Research and optimization laboratory

- **Python**
- NumPy
- SciPy
- pytest
- QUBO modeling
- exact classical solver for small cases
- greedy classical baseline
- QAOA statevector experiments
- qBraid SDK integration

The quantum layer is experimental and advisory. It does not have direct authority over Windows processes.

## Current benchmark evidence

During development, the laboratory executed:

- **40,000 simulated optimization scenarios**;
- **0 protected-process selections** in that benchmark;
- **100 exact classical comparisons**;
- the tested greedy baseline matched the exact optimum in those 100 small validation cases;
- local QAOA experiments were also executed for comparison.

These results are development evidence, not a claim of universal performance improvement or quantum advantage.

## Project architecture

```text
QSentinel/
├── src/
│   └── QSentinel.Windows/
│       ├── Program.cs
│       ├── MainForm.cs
│       ├── SystemMonitor.cs
│       ├── OptimizationEngine.cs
│       ├── SafetyEngine.cs
│       ├── NativeSystem.cs
│       ├── StartupManager.cs
│       └── AppSettings.cs
│
├── quantum/
│   ├── qubo/
│   ├── qaoa/
│   └── experiments/
│
├── simulator/
├── benchmarks/
├── tests/
├── docs/
├── installer/
└── .github/workflows/
```

## Installation

The GitHub Actions workflow builds two Windows x64 artifacts:

1. **QSentinel-Setup-Windows-x64** — recommended installer.
2. **QSentinel-Portable-Windows-x64** — portable executable.

Open the repository **Actions** tab, select the latest successful **QSentinel Windows Build**, and download the desired artifact.

The installer configures QSentinel for Windows and creates startup integration.

## Build from source

Requirements:

- Windows 10/11 x64;
- .NET 8 SDK.

```powershell
git clone https://github.com/MukaSanches/QSentinel.git
cd QSentinel

dotnet restore src/QSentinel.Windows/QSentinel.Windows.csproj

dotnet build src/QSentinel.Windows/QSentinel.Windows.csproj -c Release

dotnet publish src/QSentinel.Windows/QSentinel.Windows.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

## Safety and limitations

QSentinel is still under active development.

Important principles:

- protected Windows processes must not be optimized automatically;
- foreground applications are excluded from background optimization;
- changes should be reversible;
- automatic termination is restricted to the explicit user block list;
- performance gains depend on workload, hardware, storage, drivers, Windows configuration, and running software;
- no claim of quantum advantage is made without reproducible evidence.

---

# Português

## Visão geral

**QSentinel** é um orquestrador de recursos para Windows criado para monitorar o computador continuamente e reduzir pressão desnecessária sobre CPU, memória e disco/I/O sem simplesmente sair encerrando programas.

O projeto reúne:

- monitoramento de processos em tempo real;
- análise adaptativa da pressão do sistema;
- identificação do aplicativo que está em primeiro plano;
- otimização reversível de processos;
- regras de segurança para processos protegidos;
- inicialização automática com o Windows;
- funcionamento residente na bandeja;
- bloqueio manual de aplicativos;
- gráficos de desempenho em tempo real;
- pesquisa de otimização clássica;
- experimentos híbridos clássico/quânticos com qBraid.

O aplicativo Windows é desenvolvido em **C# / .NET 8 / Windows Forms**. A camada de pesquisa utiliza **Python**.

## O que o QSentinel 1.1 faz

### Monitoramento em tempo real

O QSentinel acompanha continuamente:

- uso total da CPU;
- pressão da memória física;
- atividade agregada de I/O;
- uso de CPU por processo;
- memória utilizada por processo;
- I/O de cada processo;
- programa que o usuário está utilizando naquele momento.

O intervalo padrão de atualização é de aproximadamente **1,5 segundo**.

### Motor adaptativo de otimização

O QSentinel calcula uma pontuação de pressão do sistema e adapta sua atuação conforme a carga atual.

Quando a pressão aumenta, ele pode selecionar processos seguros em segundo plano e aplicar controles reversíveis, como:

- prioridade **Below Normal**;
- power throttling / comportamento de eficiência do Windows;
- redução da prioridade de memória quando existe pressão elevada de RAM.

Quando a pressão diminui, o QSentinel restaura a prioridade anterior do processo.

O QSentinel **não** considera uso alto de memória, sozinho, motivo suficiente para encerrar um programa.

### Safety Engine

O Safety Engine impede a otimização automática de processos protegidos do Windows, componentes de segurança, do próprio QSentinel e do aplicativo que está em primeiro plano.

A arquitetura segue:

```text
Monitoramento
   ↓
Motor de decisão adaptativo
   ↓
Safety Engine
   ↓
Intervenção reversível no Windows
```

E não:

```text
Otimizador → encerramento irrestrito de processos
```

### Integração com a inicialização do Windows

O QSentinel pode se registrar para iniciar automaticamente com o Windows.

No login, ele pode abrir em modo de segundo plano e permanecer disponível pela área de notificação.

Ao clicar no **X**, a janela é escondida e o motor continua ativo na bandeja.

O usuário ou administrador do Windows continua podendo encerrar ou desinstalar o aplicativo.

### Bloqueio de aplicativos

O usuário pode adicionar explicitamente um processo não protegido à lista de bloqueio.

Somente aplicativos colocados intencionalmente nessa lista são encerrados automaticamente quando detectados.

Programas desconhecidos **não** são encerrados automaticamente.

### Dashboard em tempo real

A versão 1.1 inclui uma interface redesenhada com:

- indicador de CPU;
- indicador de memória;
- indicador de disco / I/O;
- quantidade de processos atualmente otimizados;
- indicador de pressão do sistema;
- gráfico histórico de CPU;
- gráfico histórico de memória;
- gráfico de atividade de I/O;
- tabela de processos com CPU, memória, I/O, PID e estado;
- estados visíveis como **PROTEGIDO**, **EM USO**, **ALTO IMPACTO**, **OBSERVAR** e **OTIMIZADO**.

## Tecnologia

### Aplicativo Windows

- **C#**
- **.NET 8**
- **Windows Forms**
- APIs nativas do Windows via P/Invoke
- integração de inicialização pelo Registro do Windows
- gerenciamento de prioridade de processos
- Windows power throttling / controle de velocidade de execução
- prioridade de memória por processo
- detecção da janela em primeiro plano
- contadores de I/O de processos
- integração com a bandeja do sistema

### Laboratório de pesquisa e otimização

- **Python**
- NumPy
- SciPy
- pytest
- modelagem QUBO
- resolvedor clássico exato para casos pequenos
- baseline clássico greedy
- experimentos QAOA em statevector
- integração com qBraid SDK

A camada quântica é experimental e consultiva. Ela não possui autoridade direta sobre processos do Windows.

## Evidências atuais de benchmark

Durante o desenvolvimento, o laboratório executou:

- **40.000 cenários simulados de otimização**;
- **0 seleções de processos protegidos** nesse benchmark;
- **100 comparações clássicas exatas**;
- o baseline greedy testado encontrou o mesmo ótimo do resolvedor exato nesses 100 pequenos casos;
- também foram executados experimentos locais com QAOA para comparação.

Esses resultados são evidências de desenvolvimento, não uma promessa de ganho universal nem uma alegação de vantagem quântica.

## Arquitetura do projeto

```text
QSentinel/
├── src/
│   └── QSentinel.Windows/
│       ├── Program.cs
│       ├── MainForm.cs
│       ├── SystemMonitor.cs
│       ├── OptimizationEngine.cs
│       ├── SafetyEngine.cs
│       ├── NativeSystem.cs
│       ├── StartupManager.cs
│       └── AppSettings.cs
│
├── quantum/
│   ├── qubo/
│   ├── qaoa/
│   └── experiments/
│
├── simulator/
├── benchmarks/
├── tests/
├── docs/
├── installer/
└── .github/workflows/
```

## Instalação

O GitHub Actions gera dois artifacts para Windows x64:

1. **QSentinel-Setup-Windows-x64** — instalador recomendado.
2. **QSentinel-Portable-Windows-x64** — executável portátil.

Abra a aba **Actions** do repositório, escolha a execução mais recente com sucesso de **QSentinel Windows Build** e baixe o artifact desejado.

O instalador configura o QSentinel no Windows e cria a integração de inicialização automática.

## Compilar a partir do código-fonte

Requisitos:

- Windows 10/11 x64;
- .NET 8 SDK.

```powershell
git clone https://github.com/MukaSanches/QSentinel.git
cd QSentinel

dotnet restore src/QSentinel.Windows/QSentinel.Windows.csproj

dotnet build src/QSentinel.Windows/QSentinel.Windows.csproj -c Release

dotnet publish src/QSentinel.Windows/QSentinel.Windows.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

## Segurança e limitações

O QSentinel continua em desenvolvimento ativo.

Princípios importantes:

- processos protegidos do Windows não devem sofrer otimização automática;
- aplicativos em primeiro plano são excluídos da otimização de fundo;
- as alterações devem ser reversíveis;
- encerramento automático é restrito à lista de bloqueio criada pelo usuário;
- ganhos de desempenho variam conforme hardware, armazenamento, drivers, configuração do Windows e programas em execução;
- nenhuma vantagem quântica é alegada sem evidência reproduzível.

---

# Español

## Descripción general

**QSentinel** es un orquestador de recursos para Windows diseñado para supervisar el sistema continuamente y reducir presión innecesaria sobre CPU, memoria y disco/I/O sin cerrar aplicaciones de forma indiscriminada.

El proyecto combina:

- supervisión de procesos en tiempo real;
- análisis adaptativo de presión del sistema;
- detección de la aplicación en primer plano;
- optimización reversible de procesos;
- reglas de seguridad para procesos protegidos;
- inicio automático con Windows;
- funcionamiento residente en el área de notificación;
- bloqueo manual de aplicaciones;
- gráficos de rendimiento en tiempo real;
- investigación de optimización clásica;
- experimentos híbridos clásico/cuánticos con qBraid.

La aplicación de Windows está desarrollada con **C# / .NET 8 / Windows Forms**. La capa de investigación utiliza **Python**.

## Qué hace QSentinel 1.1

### Supervisión en tiempo real

QSentinel mide continuamente:

- uso total de CPU;
- presión de memoria física;
- actividad agregada de I/O;
- uso de CPU por proceso;
- memoria utilizada por proceso;
- actividad I/O de cada proceso;
- aplicación que se encuentra en primer plano.

El intervalo de actualización predeterminado es de aproximadamente **1,5 segundos**.

### Motor adaptativo de optimización

QSentinel calcula una puntuación de presión del sistema y adapta su comportamiento según la carga actual.

Cuando aumenta la presión, puede seleccionar procesos seguros en segundo plano y aplicar controles reversibles como:

- prioridad **Below Normal**;
- Windows power throttling / comportamiento de eficiencia;
- reducción de prioridad de memoria cuando existe presión elevada de RAM.

Cuando la presión disminuye, QSentinel restaura la prioridad anterior del proceso.

QSentinel **no** considera que un alto uso de memoria, por sí solo, sea motivo suficiente para cerrar un programa.

### Safety Engine

El Safety Engine impide la optimización automática de procesos protegidos de Windows, componentes de seguridad, QSentinel y la aplicación que está actualmente en primer plano.

La arquitectura es:

```text
Supervisión
   ↓
Motor adaptativo de decisión
   ↓
Safety Engine
   ↓
Intervención reversible en Windows
```

No:

```text
Optimizador → cierre irrestricto de procesos
```

### Integración con el inicio de Windows

QSentinel puede registrarse para iniciarse automáticamente con Windows.

Al iniciar sesión, puede abrirse en segundo plano y permanecer disponible desde el área de notificación.

Al cerrar la ventana principal con **X**, la interfaz se oculta pero el motor continúa activo.

El usuario o administrador de Windows mantiene el control para detener o desinstalar QSentinel.

### Bloqueo de aplicaciones

El usuario puede agregar explícitamente un proceso no protegido a la lista de bloqueo.

Solo las aplicaciones añadidas intencionalmente a esa lista se cierran automáticamente cuando son detectadas.

Las aplicaciones desconocidas **no** se cierran automáticamente.

### Panel en tiempo real

La versión 1.1 incluye una interfaz rediseñada con:

- indicador de CPU;
- indicador de memoria;
- indicador de disco / I/O;
- cantidad de procesos actualmente optimizados;
- indicador de presión del sistema;
- gráfico histórico de CPU;
- gráfico histórico de memoria;
- gráfico de actividad I/O;
- tabla de procesos con CPU, memoria, I/O, PID y estado;
- estados visibles como **PROTEGIDO**, **EN USO**, **ALTO IMPACTO**, **OBSERVAR** y **OPTIMIZADO**.

## Tecnología

### Aplicación Windows

- **C#**
- **.NET 8**
- **Windows Forms**
- APIs nativas de Windows mediante P/Invoke
- integración de inicio mediante el Registro de Windows
- administración de prioridad de procesos
- Windows power throttling / control de velocidad de ejecución
- prioridad de memoria por proceso
- detección de ventana en primer plano
- contadores I/O de procesos
- integración con el área de notificación

### Laboratorio de investigación y optimización

- **Python**
- NumPy
- SciPy
- pytest
- modelado QUBO
- solucionador clásico exacto para casos pequeños
- baseline clásico greedy
- experimentos QAOA statevector
- integración con qBraid SDK

La capa cuántica es experimental y consultiva. No tiene autoridad directa sobre los procesos de Windows.

## Evidencia actual de benchmark

Durante el desarrollo, el laboratorio ejecutó:

- **40.000 escenarios simulados de optimización**;
- **0 selecciones de procesos protegidos** en ese benchmark;
- **100 comparaciones clásicas exactas**;
- el baseline greedy probado encontró el mismo óptimo que el solucionador exacto en esos 100 casos pequeños;
- también se ejecutaron experimentos locales con QAOA para comparación.

Estos resultados son evidencia de desarrollo, no una promesa de mejora universal ni una afirmación de ventaja cuántica.

## Arquitectura del proyecto

```text
QSentinel/
├── src/
│   └── QSentinel.Windows/
│       ├── Program.cs
│       ├── MainForm.cs
│       ├── SystemMonitor.cs
│       ├── OptimizationEngine.cs
│       ├── SafetyEngine.cs
│       ├── NativeSystem.cs
│       ├── StartupManager.cs
│       └── AppSettings.cs
│
├── quantum/
│   ├── qubo/
│   ├── qaoa/
│   └── experiments/
│
├── simulator/
├── benchmarks/
├── tests/
├── docs/
├── installer/
└── .github/workflows/
```

## Instalación

GitHub Actions genera dos artifacts para Windows x64:

1. **QSentinel-Setup-Windows-x64** — instalador recomendado.
2. **QSentinel-Portable-Windows-x64** — ejecutable portátil.

Abra la pestaña **Actions** del repositorio, seleccione la ejecución exitosa más reciente de **QSentinel Windows Build** y descargue el artifact deseado.

El instalador configura QSentinel en Windows y crea la integración de inicio automático.

## Compilar desde el código fuente

Requisitos:

- Windows 10/11 x64;
- .NET 8 SDK.

```powershell
git clone https://github.com/MukaSanches/QSentinel.git
cd QSentinel

dotnet restore src/QSentinel.Windows/QSentinel.Windows.csproj

dotnet build src/QSentinel.Windows/QSentinel.Windows.csproj -c Release

dotnet publish src/QSentinel.Windows/QSentinel.Windows.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true
```

## Seguridad y limitaciones

QSentinel continúa en desarrollo activo.

Principios importantes:

- los procesos protegidos de Windows no deben recibir optimización automática;
- las aplicaciones en primer plano quedan excluidas de la optimización en segundo plano;
- los cambios deben ser reversibles;
- el cierre automático está restringido a la lista de bloqueo creada por el usuario;
- las mejoras de rendimiento dependen del hardware, almacenamiento, controladores, configuración de Windows y software en ejecución;
- no se afirma ninguna ventaja cuántica sin evidencia reproducible.

---

## Repository

**GitHub:** https://github.com/MukaSanches/QSentinel

**Current Windows version:** 1.1.0

**Main branch:** `main`
