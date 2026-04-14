#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_FILE="$ROOT_DIR/AuthCore.sln"
BACKEND_DIR="$ROOT_DIR/src/Backend"
COMPOSE_FILE="$BACKEND_DIR/docker-compose.yml"
ENV_FILE="$BACKEND_DIR/.env.development"
API_PROJECT="$BACKEND_DIR/AuthCore/AuthCore.Api/AuthCore.Api.csproj"
DEFAULT_COMMAND="${1:-dev}"

GREEN='\033[1;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'
INFRA_STARTED=false

compose() {
    if command -v docker-compose >/dev/null 2>&1; then
        docker-compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "$@"
        return
    fi

    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" "$@"
}

ensure_command() {
    local command_name="$1"

    if ! command -v "$command_name" >/dev/null 2>&1; then
        echo -e "${RED}Erro:${NC} comando '$command_name' nao foi encontrado."
        exit 1
    fi
}

ensure_file() {
    local file_path="$1"

    if [ ! -f "$file_path" ]; then
        echo -e "${RED}Erro:${NC} arquivo nao encontrado: $file_path"
        exit 1
    fi
}

print_usage() {
    cat <<EOF
Uso:
  ./run.sh [comando]

Comandos:
  dev       Sobe postgres, redis e rabbitmq com Docker e executa a API localmente.
  watch     Igual ao dev, mas usa dotnet watch.
  infra     Sobe apenas a infraestrutura em background.
  docker    Sobe toda a aplicacao com Docker Compose.
  build     Compila a solucao.
  test      Executa os testes da solucao.
  down      Encerra os containers do docker compose.
  help      Exibe esta ajuda.

Exemplos:
  ./run.sh
  ./run.sh watch
  ./run.sh docker
  ./run.sh down
EOF
}

cleanup() {
    if [ "$INFRA_STARTED" != true ]; then
        return
    fi

    INFRA_STARTED=false

    echo
    echo -e "${YELLOW}Encerrando infraestrutura...${NC}"
    compose down --remove-orphans
    echo -e "${GREEN}Infraestrutura encerrada.${NC}"
}

start_infra() {
    ensure_command docker
    ensure_file "$COMPOSE_FILE"
    ensure_file "$ENV_FILE"

    echo -e "${YELLOW}Subindo infraestrutura...${NC}"
    compose up -d postgres redis rabbitmq
    INFRA_STARTED=true
    echo -e "${GREEN}Infraestrutura pronta.${NC}"
}

run_api() {
    ensure_command dotnet
    ensure_file "$API_PROJECT"

    export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

    echo -e "${YELLOW}Executando AuthCore.Api localmente...${NC}"
    dotnet run --project "$API_PROJECT" --launch-profile http
}

watch_api() {
    ensure_command dotnet
    ensure_file "$API_PROJECT"

    export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

    echo -e "${YELLOW}Executando AuthCore.Api com hot reload...${NC}"
    dotnet watch --project "$API_PROJECT" run --launch-profile http
}

run_docker() {
    ensure_command docker
    ensure_file "$COMPOSE_FILE"
    ensure_file "$ENV_FILE"

    echo -e "${YELLOW}Subindo aplicacao completa com Docker Compose...${NC}"
    compose up --build
}

build_solution() {
    ensure_command dotnet
    ensure_file "$SOLUTION_FILE"

    echo -e "${YELLOW}Compilando solucao...${NC}"
    dotnet build "$SOLUTION_FILE"
}

test_solution() {
    ensure_command dotnet
    ensure_file "$SOLUTION_FILE"

    echo -e "${YELLOW}Executando testes...${NC}"
    dotnet test "$SOLUTION_FILE"
}

stop_containers() {
    ensure_command docker
    ensure_file "$COMPOSE_FILE"
    ensure_file "$ENV_FILE"

    echo -e "${YELLOW}Encerrando containers...${NC}"
    compose down --remove-orphans
    echo -e "${GREEN}Containers encerrados.${NC}"
}

case "$DEFAULT_COMMAND" in
    dev)
        start_infra
        trap cleanup EXIT INT TERM
        run_api
        ;;
    watch)
        start_infra
        trap cleanup EXIT INT TERM
        watch_api
        ;;
    infra)
        start_infra
        ;;
    docker)
        run_docker
        ;;
    build)
        build_solution
        ;;
    test)
        test_solution
        ;;
    down)
        stop_containers
        ;;
    help|-h|--help)
        print_usage
        ;;
    *)
        echo -e "${RED}Erro:${NC} comando invalido '$DEFAULT_COMMAND'."
        echo
        print_usage
        exit 1
        ;;
esac
