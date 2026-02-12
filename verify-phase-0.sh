#!/bin/bash

# =======================================================================================
# IODA CMS - Script de Verificación de la Fase 0
# =======================================================================================
# Este script verifica que todos los componentes de la Fase 0 estén correctamente
# configurados y funcionando.
# =======================================================================================

echo "🔍 Verificando Fase 0 - IODA CMS"
echo "=================================="
echo ""

# Colores para output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Contador de verificaciones
PASSED=0
FAILED=0

# Función para verificar
check() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✅ $1${NC}"
        ((PASSED++))
    else
        echo -e "${RED}❌ $1${NC}"
        ((FAILED++))
    fi
}

# 1. Verificar .NET SDK
echo "1️⃣ Verificando .NET SDK..."
dotnet --version > /dev/null 2>&1
check "SDK .NET 9 instalado"
echo ""

# 2. Verificar estructura de directorios
echo "2️⃣ Verificando estructura de directorios..."
[ -d "src/Shared/IODA.Shared.Contracts" ] && check "Directorio IODA.Shared.Contracts existe" || check "Directorio IODA.Shared.Contracts NO existe"
[ -d "src/Shared/IODA.Shared.BuildingBlocks" ] && check "Directorio IODA.Shared.BuildingBlocks existe" || check "Directorio IODA.Shared.BuildingBlocks NO existe"
[ -d "src/Shared/IODA.Shared.Infrastructure" ] && check "Directorio IODA.Shared.Infrastructure existe" || check "Directorio IODA.Shared.Infrastructure NO existe"
[ -d "docs" ] && check "Directorio docs existe" || check "Directorio docs NO existe"
echo ""

# 3. Verificar archivos de configuración
echo "3️⃣ Verificando archivos de configuración..."
[ -f "IODA.sln" ] && check "Archivo IODA.sln existe" || check "Archivo IODA.sln NO existe"
[ -f "global.json" ] && check "Archivo global.json existe" || check "Archivo global.json NO existe"
[ -f "Directory.Build.props" ] && check "Archivo Directory.Build.props existe" || check "Archivo Directory.Build.props NO existe"
[ -f ".editorconfig" ] && check "Archivo .editorconfig existe" || check "Archivo .editorconfig NO existe"
[ -f "docker-compose.yml" ] && check "Archivo docker-compose.yml existe" || check "Archivo docker-compose.yml NO existe"
echo ""

# 4. Verificar documentación
echo "4️⃣ Verificando documentación..."
[ -f "README.md" ] && check "README.md existe" || check "README.md NO existe"
[ -f "PLAN_DE_TRABAJO.md" ] && check "PLAN_DE_TRABAJO.md existe" || check "PLAN_DE_TRABAJO.md NO existe"
[ -f "docs/CONVENTIONS.md" ] && check "docs/CONVENTIONS.md existe" || check "docs/CONVENTIONS.md NO existe"
[ -f "docs/EVENTS.md" ] && check "docs/EVENTS.md existe" || check "docs/EVENTS.md NO existe"
[ -f "docs/FASE_0_COMPLETADA.md" ] && check "docs/FASE_0_COMPLETADA.md existe" || check "docs/FASE_0_COMPLETADA.md NO existe"
[ -f "NEXT_STEPS.md" ] && check "NEXT_STEPS.md existe" || check "NEXT_STEPS.md NO existe"
echo ""

# 5. Compilar proyectos
echo "5️⃣ Compilando proyectos Shared..."
echo "   ⏳ Compilando IODA.Shared.BuildingBlocks..."
dotnet build src/Shared/IODA.Shared.BuildingBlocks/IODA.Shared.BuildingBlocks.csproj --nologo --verbosity quiet > /dev/null 2>&1
check "IODA.Shared.BuildingBlocks compila correctamente"

echo "   ⏳ Compilando IODA.Shared.Contracts..."
dotnet build src/Shared/IODA.Shared.Contracts/IODA.Shared.Contracts.csproj --nologo --verbosity quiet > /dev/null 2>&1
check "IODA.Shared.Contracts compila correctamente"

echo "   ⏳ Compilando IODA.Shared.Infrastructure..."
dotnet build src/Shared/IODA.Shared.Infrastructure/IODA.Shared.Infrastructure.csproj --nologo --verbosity quiet > /dev/null 2>&1
check "IODA.Shared.Infrastructure compila correctamente"
echo ""

# 6. Verificar Building Blocks
echo "6️⃣ Verificando Building Blocks..."
[ -f "src/Shared/IODA.Shared.BuildingBlocks/Domain/Entity.cs" ] && check "Entity.cs existe" || check "Entity.cs NO existe"
[ -f "src/Shared/IODA.Shared.BuildingBlocks/Domain/AggregateRoot.cs" ] && check "AggregateRoot.cs existe" || check "AggregateRoot.cs NO existe"
[ -f "src/Shared/IODA.Shared.BuildingBlocks/Domain/ValueObject.cs" ] && check "ValueObject.cs existe" || check "ValueObject.cs NO existe"
[ -f "src/Shared/IODA.Shared.BuildingBlocks/Domain/IDomainEvent.cs" ] && check "IDomainEvent.cs existe" || check "IDomainEvent.cs NO existe"
[ -f "src/Shared/IODA.Shared.BuildingBlocks/Domain/Specification.cs" ] && check "Specification.cs existe" || check "Specification.cs NO existe"
echo ""

# 7. Verificar Contratos de Eventos
echo "7️⃣ Verificando Contratos de Eventos..."
[ -f "src/Shared/IODA.Shared.Contracts/Events/IEvent.cs" ] && check "IEvent.cs existe" || check "IEvent.cs NO existe"
[ -f "src/Shared/IODA.Shared.Contracts/Events/EventBase.cs" ] && check "EventBase.cs existe" || check "EventBase.cs NO existe"
[ -f "src/Shared/IODA.Shared.Contracts/Events/V1/ContentCreatedEventV1.cs" ] && check "ContentCreatedEventV1.cs existe" || check "ContentCreatedEventV1.cs NO existe"
[ -f "src/Shared/IODA.Shared.Contracts/Events/V1/ContentPublishedEventV1.cs" ] && check "ContentPublishedEventV1.cs existe" || check "ContentPublishedEventV1.cs NO existe"
echo ""

# Resumen
echo "=================================="
echo "📊 Resumen de Verificación"
echo "=================================="
echo -e "${GREEN}✅ Verificaciones exitosas: $PASSED${NC}"
if [ $FAILED -gt 0 ]; then
    echo -e "${RED}❌ Verificaciones fallidas: $FAILED${NC}"
else
    echo -e "${GREEN}❌ Verificaciones fallidas: 0${NC}"
fi
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 ¡FASE 0 COMPLETADA EXITOSAMENTE!${NC}"
    echo ""
    echo "✅ Todos los componentes están en su lugar"
    echo "✅ Los proyectos compilan correctamente"
    echo "✅ La documentación está completa"
    echo ""
    echo "🚀 El proyecto está listo para la Fase 1"
    exit 0
else
    echo -e "${RED}⚠️  Hay algunos problemas que requieren atención${NC}"
    exit 1
fi
