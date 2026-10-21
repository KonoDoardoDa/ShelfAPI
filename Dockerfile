# =============================================================
# DOCKERFILE MULTI-STAGE: SHELFAPI (.NET 9.0 WEB API)
# =============================================================

# -------------------------------------------------------------
# ESTÁGIO 1: Runtime Base
# Usamos a imagem oficial 'aspnet:9.0' que contém apenas o runtime
# necessário para executar a aplicação (sem ferramentas pesadas de SDK).
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# No .NET 8 e 9, o runtime escuta nativamente na porta 8080 (não-root)
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# -------------------------------------------------------------
# ESTÁGIO 2: Build do Projeto
# Usamos a imagem oficial 'sdk:9.0' com compiladores e ferramentas dotnet.
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Passo estratégico de Cache:
# Copiamos primeiro APENAS o .csproj e rodamos o restore das dependências.
# O Docker armazena isso em cache. Se você alterar apenas código C#, o restore não roda de novo!
COPY ["ShelfApi.csproj", "./"]
RUN dotnet restore "ShelfApi.csproj"

# Copia todo o restante dos arquivos do projeto
COPY . .
WORKDIR "/src"
RUN dotnet build "ShelfApi.csproj" -c Release -o /app/build

# -------------------------------------------------------------
# ESTÁGIO 3: Publicação dos Binários
# Gera a versão final compilada e minificada para produção.
# -------------------------------------------------------------
FROM build AS publish
RUN dotnet publish "ShelfApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -------------------------------------------------------------
# ESTÁGIO 4: Imagem Final Limpa (Produção)
# Copia somente os binários prontos para a imagem leve do Estágio 1.
# O SDK e arquivos temporários de build são descartados.
# -------------------------------------------------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShelfApi.dll"]
