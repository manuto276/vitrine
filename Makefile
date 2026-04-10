PROJECT := src/Vitrine.Engine
PUBLISH_DIR := publish
RID := win-x64

build:
	dotnet build $(PROJECT) -c Release

publish:
	dotnet publish $(PROJECT) -c Release -r $(RID) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-o $(PUBLISH_DIR)

restore:
	dotnet restore $(PROJECT)

clean:
	dotnet clean $(PROJECT)
	rm -rf $(PUBLISH_DIR)

.PHONY: build publish restore clean