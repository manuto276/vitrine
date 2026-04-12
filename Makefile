PROJECT := src/Vitrine.Engine
PUBLISH_DIR := publish
RID := win-x64
THEMES_SRC := src/themes
THEMES_DEST := $(PROJECT)/Assets/themes

build: build-themes
	dotnet build $(PROJECT) -c Release

debug: build-themes
	dotnet publish $(PROJECT) -c Debug -r $(RID) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-o $(PUBLISH_DIR)/debug

release: build-themes
	dotnet publish $(PROJECT) -c Release -r $(RID) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-o $(PUBLISH_DIR)/release

restore:
	dotnet restore $(PROJECT)

build-themes:
	@for dir in $(THEMES_SRC)/*/; do \
		name=$$(basename "$$dir"); \
		echo "Building theme: $$name"; \
		cd "$$dir" && npm install --silent && npm run build --silent && cd -; \
		mkdir -p "$(THEMES_DEST)/$$name"; \
		cp "$$dir/dist/theme.js" "$(THEMES_DEST)/$$name/theme.js"; \
		cp "$$dir/dist/theme.css" "$(THEMES_DEST)/$$name/theme.css" 2>/dev/null || true; \
		cp "$$dir/theme.json" "$(THEMES_DEST)/$$name/theme.json" 2>/dev/null || true; \
		cp "$$dir/src/settings.json" "$(THEMES_DEST)/$$name/settings.json" 2>/dev/null || true; \
		cp "$$dir/src/settings.definitions.json" "$(THEMES_DEST)/$$name/settings.definitions.json" 2>/dev/null || true; \
	done

clean:
	dotnet clean $(PROJECT) -c Release
	dotnet clean $(PROJECT) -c Debug
	rm -rf $(PUBLISH_DIR)

.PHONY: build debug release restore clean build-themes
