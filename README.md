# FCG — Notifications API

Microsserviço de **notificações** da FIAP Cloud Games. É orientado a eventos e **stateless**: consome `UserCreatedEvent` e `PaymentProcessedEvent` e "envia" e-mails — simulados por log no console, conforme o desafio. Expõe HTTP apenas para as probes do Kubernetes.

> Parte da arquitetura de microsserviços da FIAP Cloud Games. Visão geral, diagramas, decisões (ADRs) e matriz de
> requisitos no repositório [fcg-orchestration](https://github.com/Phsynnistro/fcg-orchestration).

**Stack:** .NET 10 · ASP.NET Core Minimal APIs · MassTransit 8.5 + RabbitMQ · OpenTelemetry · xUnit

## Responsabilidades

- `UserCreatedEvent` → e-mail de boas-vindas.
- `PaymentProcessedEvent` com `Approved` → e-mail de confirmação de compra (recusados são apenas registrados no log).

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `GET` | `/health/live · /health/ready · /health` | Anônimo | Probes e diagnóstico; aqui a readiness inclui a conexão com o RabbitMQ |
| `GET` | `/` | Anônimo | Identificação do serviço |

Todos os serviços expõem `/health/live` (liveness), `/health/ready` (readiness) e `/health` (diagnóstico em JSON).
Erros seguem **Problem Details (RFC 9457)** com `traceId`.

## Eventos

| Direção | Evento | Exchange | Fila deste serviço | Campos principais |
|---|---|---|---|---|
| Consome | `UserCreatedEvent` | `fcg.user-created` | `notifications.user-created` | `UserId`, `Name`, `Email`, `CreatedAt` |
| Consome | `PaymentProcessedEvent` | `fcg.payment-processed` | `notifications.payment-processed` | `Status`, `UserEmail`, `GameTitle`, `Price`, `OrderId` |

Contratos em `src/Fcg.Notifications.Api/Contracts/Events`, namespace **`Fcg.Contracts.Events`** — idêntico em todos os
repositórios (o MassTransit identifica a mensagem por namespace + nome). Altere-os apenas de forma **aditiva**;
o `ContractTests` falha se a identidade do contrato mudar.

## Variáveis de ambiente

Configuração .NET via variáveis de ambiente: `__` (dois underscores) equivale a `:` — `RabbitMq__Host` → `RabbitMq:Host`.

| Variável | Exemplo | Origem no K8s | Descrição |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | ConfigMap | Ambiente do ASP.NET Core. |
| `RabbitMq__Host` | `rabbitmq` | ConfigMap | Host do broker (nome do Service no cluster). |
| `RabbitMq__Port` | `5672` | ConfigMap | Porta AMQP. |
| `RabbitMq__VirtualHost` | `/` | ConfigMap | Virtual host do RabbitMQ. |
| `RabbitMq__Username` | `fcg` | **Secret** | Usuário do broker. |
| `RabbitMq__Password` | `••••` | **Secret** | Senha do broker. |
| `Messaging__Queues__UserCreated` | `notifications.user-created` | ConfigMap | Fila para `UserCreatedEvent`. |
| `Messaging__Queues__PaymentProcessed` | `notifications.payment-processed` | ConfigMap | Fila para `PaymentProcessedEvent` (diferente da fila do Catalog: fan-out). |
| `Messaging__PrefetchCount` | `16` | ConfigMap | Mensagens pré-buscadas por consumidor. |
| `Messaging__ConcurrentMessageLimit` | `8` | ConfigMap | Processamento concorrente por réplica (bulkhead). |
| `Email__From` | `no-reply@fiapcloudgames.com` | ConfigMap | Remetente dos e-mails. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://jaeger:4317` | ConfigMap | Coletor OTLP (Jaeger). Ausente ⇒ tracing não é exportado. |

Valores sensíveis (**Secret**) nunca entram no `appsettings.json`. O `appsettings.Development.json` contém valores
**apenas de desenvolvimento local**.

## Executar localmente

```bash
# 1) infraestrutura (a partir do repositório de orquestração)
cd ../fcg-orchestration && docker compose up -d rabbitmq jaeger

# 2) o serviço
cd ../fcg-notifications-api
dotnet run --project src/Fcg.Notifications.Api        # http://localhost:5104
```

## Testes

```bash
dotnet test
```

Testes de consumer com o **MassTransit test harness** e de **contrato**.

## Docker

```bash
docker build -t fcg/notifications-api:1.0.0 .            # imagem final: runtime chiseled, não-root, porta 8080
docker build --target test .                    # (opcional) roda os testes dentro do build
```

Dockerfile **multi-stage**: `build` (SDK) → `test` (opcional) → `final` (`aspnet:10.0-noble-chiseled`).
O CI analisa a imagem com **Trivy** e falha em vulnerabilidade CRITICAL/HIGH corrigível.

## Kubernetes

| Arquivo em [`k8s/`](k8s) | Conteúdo |
|---|---|
| `00-namespace.yaml` | Namespace `fcg` (permite aplicar este repositório isoladamente) |
| `01-configmap.yaml` | ConfigMap: filas, URLs de serviços, limites |
| `02-secrets.yaml` | Secrets: credenciais |
| `04-deployment.yaml` | Deployment da API: probes, recursos, securityContext, anotações |
| `05-service.yaml` | Service ClusterIP `notifications-api:80` |
| `06-hpa.yaml` | HorizontalPodAutoscaler: 2–5 réplicas por CPU |

```bash
# pré-requisito: RabbitMQ e Jaeger do repositório fcg-orchestration (ou o ambiente completo dele)
cd k8s && kubectl apply -f .
kubectl -n fcg get pods -l app.kubernetes.io/name=notifications-api
```

Este `/k8s` é a **fonte da verdade** do serviço. O repositório de orquestração mantém uma cópia sincronizada
(`scripts/k8s-sync-manifests.sh`) para subir o ambiente inteiro com um único `kubectl apply -f .` (ADR-0013).

## Estrutura

```
src/Fcg.Notifications.Api/
├── Contracts/Events/                UserCreatedEvent, PaymentProcessedEvent
├── Application/Abstractions/        IEmailSender (porta)
├── Application/Notifications/       EmailTemplates
├── Infrastructure/Email/            ConsoleEmailSender (adapter)
├── Infrastructure/Messaging/        opções de filas e do RabbitMQ
├── Infrastructure/Observability/    OpenTelemetry
└── Presentation/                    consumers + health
tests/Fcg.Notifications.UnitTests/
k8s/                                 manifestos Kubernetes (00 a 06)
Dockerfile · Fcg.Notifications.sln · Directory.Build.props · Directory.Packages.props (versões centralizadas) · global.json
```

## Decisões de design

- **Fan-out**: a fila `notifications.payment-processed` é distinta de `catalog.payment-processed`; ambas recebem cópia do mesmo evento.
- **Readiness = conectado ao broker** (health check do MassTransit): o rolling update só remove a réplica antiga quando a nova já consome.
- **Porta `IEmailSender`**: o adapter atual escreve no log; trocar por SMTP/SendGrid/Azure Communication Services não altera os consumers.
- **Templates como funções puras** (`EmailTemplates`): testáveis sem infraestrutura.
- **Sem banco ⇒ sem inbox**: em reentrega, um e-mail pode sair duplicado. Trade-off consciente e documentado (ADR-0003/0006).
- Testes de consumer com o **test harness do MassTransit** (transporte em memória, sem RabbitMQ).
