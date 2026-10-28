.PHONY: help graph-engine-build graph-engine-test graph-engine-benchmark graph-engine-benchmark-smoke api api-restore api-tools api-build api-format api-format-check api-test api-test-fast api-test-postgres api-migration api-migrate web web-build web-format web-format-check web-lint web-test web-e2e web-typecheck docs test lint format format-check quality dev docker-up docker-down clean start-api watch-api run-web start-full

SOLUTION := Coordina.sln
API_PROJECT := src/api/Coordina.Api.csproj
API_TEST_PROJECT := tests/api/Coordina.Api.Tests.csproj
API_POSTGRES_TEST_PROJECT := tests/api-postgres/Coordina.Api.Postgres.Tests.csproj
API_URL := http://localhost:5050
WEB_DIR := src/web
DOCS_DIR := docs
DOCKER_COMPOSE := docker compose --env-file .env -f docker/docker-compose.yml -p coordina
GRAPH_ENGINE_DIR := src/graph-engine
GRAPH_ENGINE_OUT := $(GRAPH_ENGINE_DIR)/bin
GRAPH_ENGINE_TEST_OUT := tests/graph-engine/bin
GRAPH_ENGINE_SOURCES := $(GRAPH_ENGINE_DIR)/src/interop/coordina_graph_engine.cpp $(GRAPH_ENGINE_DIR)/src/interop/native_bridge.cpp $(GRAPH_ENGINE_DIR)/src/core/graph_snapshot.cpp $(GRAPH_ENGINE_DIR)/src/core/graph_types.cpp $(GRAPH_ENGINE_DIR)/src/algorithms/critical_path.cpp $(GRAPH_ENGINE_DIR)/src/algorithms/dependency_validator.cpp $(GRAPH_ENGINE_DIR)/src/algorithms/graph_traversal.cpp $(GRAPH_ENGINE_DIR)/src/algorithms/node_analysis.cpp $(GRAPH_ENGINE_DIR)/src/algorithms/suggestion_engine.cpp $(GRAPH_ENGINE_DIR)/src/algorithms/topological_order.cpp $(GRAPH_ENGINE_DIR)/src/planning/planning_analysis.cpp $(GRAPH_ENGINE_DIR)/src/planning/work_planner.cpp $(GRAPH_ENGINE_DIR)/src/planning/work_score.cpp
GRAPH_ENGINE_FLAGS := -std=c++20 -O2 -fPIC -Wall -Wextra -Wpedantic -Werror -I$(GRAPH_ENGINE_DIR)/include -I$(GRAPH_ENGINE_DIR)/src

help:
	@echo "Commandes disponibles :"
	@echo "  make dev          Lance l'API C# et le front React"
	@echo "  make graph-engine-test Lance les tests natifs du moteur de graphe C++"
	@echo "  make graph-engine-benchmark Mesure l'analyse de board native (sizes=\"100 1000\" optionnel)"
	@echo "  make api          Lance l'API C#"
	@echo "  make api-watch    Lance l'API C# en mode watch"
	@echo "  make api-restore  Restaure les dependances .NET"
	@echo "  make api-tools    Restaure les outils .NET locaux"
	@echo "  make api-migration name=Nom  Cree une migration EF Core"
	@echo "  make api-migrate  Applique les migrations EF Core"
	@echo "  make api-test     Lance tous les tests .NET (rapides + PostgreSQL)"
	@echo "  make api-test-fast Lance les tests .NET en memoire"
	@echo "  make api-test-postgres Lance les tests .NET sur PostgreSQL reel (Docker requis)"
	@echo "  make web          Lance le front React"
	@echo "  make web-build    Build le front React"
	@echo "  make web-test     Lance les tests React"
	@echo "  make web-e2e      Lance les tests end-to-end Playwright"
	@echo "  make lint         Lance les linters"
	@echo "  make test         Lance tous les tests"
	@echo "  make docs         Lance la documentation Mintlify"
	@echo "  make quality      Lance format-check, lint, tests et builds"
	@echo "  make docker-up    Lance les services Docker"
	@echo "  make docker-down  Stoppe les services Docker"
	@echo "  make clean        Supprime les conteneurs, volumes et images Docker"

graph-engine-build:
	mkdir -p $(GRAPH_ENGINE_OUT)
	g++ $(GRAPH_ENGINE_FLAGS) -shared $(GRAPH_ENGINE_SOURCES) -o $(GRAPH_ENGINE_OUT)/libcoordina_graph_engine.so

graph-engine-test:
	mkdir -p $(GRAPH_ENGINE_TEST_OUT)
	g++ $(GRAPH_ENGINE_FLAGS) tests/graph-engine/graph_engine_tests.cpp $(GRAPH_ENGINE_SOURCES) -o $(GRAPH_ENGINE_TEST_OUT)/graph_engine_tests
	$(GRAPH_ENGINE_TEST_OUT)/graph_engine_tests

graph-engine-benchmark:
	mkdir -p $(GRAPH_ENGINE_TEST_OUT)
	g++ $(GRAPH_ENGINE_FLAGS) tests/graph-engine/graph_engine_benchmarks.cpp $(GRAPH_ENGINE_SOURCES) -o $(GRAPH_ENGINE_TEST_OUT)/graph_engine_benchmarks
	$(GRAPH_ENGINE_TEST_OUT)/graph_engine_benchmarks $(sizes)

graph-engine-benchmark-smoke:
	$(MAKE) graph-engine-benchmark sizes="100 1000"

api: api-migrate
	dotnet run --project $(API_PROJECT) --urls $(API_URL)

api-watch: api-migrate
	dotnet watch --project $(API_PROJECT) --urls $(API_URL)

api-restore:
	dotnet restore $(SOLUTION) -m:1

api-tools:
	dotnet tool restore

api-build: api-restore
	dotnet build $(SOLUTION) --configuration Release --no-restore -m:1

api-format: api-restore
	dotnet format $(SOLUTION) --no-restore

api-format-check: api-restore
	dotnet format $(SOLUTION) --no-restore --verify-no-changes

api-test: api-restore
	dotnet test $(SOLUTION) --configuration Release --no-restore -m:1 --collect:"XPlat Code Coverage"

api-test-fast: api-restore
	dotnet test $(API_TEST_PROJECT) --configuration Release --no-restore -m:1

api-test-postgres: api-restore
	dotnet test $(API_POSTGRES_TEST_PROJECT) --configuration Release --no-restore -m:1

api-migration: api-restore api-tools
	dotnet tool run dotnet-ef migrations add $(name) --project $(API_PROJECT) --output-dir core/Persistence/Migrations --namespace Coordina.Api.Core.Persistence.Migrations

api-migrate: api-restore api-tools
	dotnet tool run dotnet-ef database update --project $(API_PROJECT)

web:
	pnpm --dir $(WEB_DIR) dev

web-build:
	pnpm --dir $(WEB_DIR) build

web-format:
	pnpm --dir $(WEB_DIR) format

web-format-check:
	pnpm --dir $(WEB_DIR) format:check

web-lint:
	pnpm --dir $(WEB_DIR) lint

web-test:
	pnpm --dir $(WEB_DIR) test

web-e2e: api-migrate
	pnpm --dir $(WEB_DIR) e2e

web-typecheck:
	pnpm --dir $(WEB_DIR) typecheck

docs:
	cd $(DOCS_DIR) && mint dev

test: graph-engine-test graph-engine-benchmark-smoke api-test web-test web-e2e

lint: api-format-check web-lint web-typecheck

format: api-format web-format

format-check: api-format-check web-format-check

quality: format-check lint test api-build web-build

dev:
	$(MAKE) -j2 api-watch web

docker-up:
	$(DOCKER_COMPOSE) up -d --build

docker-down:
	$(DOCKER_COMPOSE) down --remove-orphans

clean:
	$(DOCKER_COMPOSE) down -v --rmi all --remove-orphans

start-api: api
watch-api: api-watch
run-web: web
start-full: docker-up
