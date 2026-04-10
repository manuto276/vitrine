PROJECT := src/Vitrine.Engine
PUBLISH_DIR := publish
RID := win-x64
THEMES_SRC := src/themes
THEMES_DEST := $(PROJECT)/Assets/themes

build: build-themes
	dotnet build $(PROJECT) -c Release

publish: build-themes
	dotnet publish $(PROJECT) -c Release -r $(RID) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-o $(PUBLISH_DIR)

restore:
	dotnet restore $(PROJECT)

build-themes:
	@for dir in $(THEMES_SRC)/*/; do \
		name=$$(basename "$$dir"); \
		echo "Building theme: $$name"; \
		cd "$$dir" && npm install --silent && npm run build --silent && cd -; \
		mkdir -p "$(THEMES_DEST)/$$name"; \
		cp "$$dir/dist/theme.js" "$(THEMES_DEST)/$$name/theme.js"; \
		cp "$$dir/theme.json" "$(THEMES_DEST)/$$name/theme.json" 2>/dev/null || true; \
	done

clean:
	dotnet clean $(PROJECT)
	rm -rf $(PUBLISH_DIR)

.PHONY: build publish restore clean build-themes
