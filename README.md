
# CBLAnki – Automação com AnkiConnect

Este projeto é uma aplicação Windows Forms em C# .NET que automatiza a criação de cartões no Anki utilizando a API AnkiConnect.
Ele permite adicionar frases em inglês e suas traduções em português, junto com áudios correspondentes, enviando tudo de forma automática para o deck selecionado.

Devido aos meus estudos de inglês com o curso do (não é uma publicidade (Mairo Vergara)), senti a necessidade para criar essa ferramenta que me ajudou a otimizar meu tempo de estudos.


## Autores

- [@pedromiguel33](https://github.com/PedroMiguel33)


## Funcionalidades

- Seleção múltipla de arquivos de áudio .mp3
- Associação automática de frases (Inglês → Português) com os áudios
- Criação de cartões no modelo Básico (digite a resposta)
- Envio automático via AnkiConnect (localhost:8765)
- Feedback visual de status (conectado, aguardando, falha, sucesso)
- Interface amigável feita em WinForms + GunaUI
## 

## Configuração
    De antemão necessitamos configurar nosso Anki para que não haja erros durante o uso do programa. 
    
    Passo-a-Passo abaixo:
    1. Vá em (Tool -> Add-ons -> Get Add-ons) e cole o código: 2055492159
    2. Logo após, vá em (Tool -> Manage Note Types)
    3. Adicione um novo "Note Type" e coloque dois campos com dessa forma (1: Frente, 2: Verso) e salve com o nome "Básico (digite a resposta)"

## 

## Como funciona

- Cole frases no campo de texto (sempre em pares: Inglês / Tradução).
- Selecione os áudios correspondentes (Ctrl+A para selecionar todos).
- Escolha o deck na lista carregada via AnkiConnect.
- Clique em Enviar e pronto — os cartões são criados automaticamente no Anki.
## 

## Stack utilizada

- **Linguagem:** C#
- **Framework:** .NET 6
- **Interface:** Windows Forms + GunaUI
- **Integração:** AnkiConnect (via HTTP/JSON)
- **IDE:** Visual Studio Pro

##

