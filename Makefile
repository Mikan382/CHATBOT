# Makefile - Prn222 Chatbot
# Chạy `make help` để xem tất cả lệnh.

SLN         := Prn222Chatbot.sln
WEB         := src/PresentationLayer
DAL         := src/DataAccessLayer
CONN        := Server=(localdb)\MSSQLLocalDB;Database=Prn222RagChatbot;Trusted_Connection=True;TrustServerCertificate=True

.DEFAULT_GOAL := help

.PHONY: help restore build run watch clean rebuild test format \
        migrate db-update db-drop secrets

help: ## Hiện danh sách lệnh
	@echo 'Prn222 Chatbot - cac lenh:'
	@echo '  make restore    - khoi phuc package'
	@echo '  make build      - build toan bo solution'
	@echo '  make run        - chay web app tai http://localhost:5096'
	@echo '  make watch      - chay web app + hot reload'
	@echo '  make clean      - xoa bin/obj'
	@echo '  make rebuild    - clean + build'
	@echo '  make test       - chay unit test'
	@echo '  make format     - format code'
	@echo '  make secrets    - tao user-secrets mac dinh (admin/AI keys)'
	@echo '  make migrate name=Ten   - tao migration moi'
	@echo '  make db-update  - ap dung migration vao DB'
	@echo '  make db-drop    - xoa database'

restore: ## Khôi phục package
	dotnet restore $(SLN)

build: ## Build solution
	dotnet build $(SLN)

run: ## Chạy web app
	dotnet run --project $(WEB)

watch: ## Chạy web app kèm hot reload
	dotnet watch --project $(WEB) run

clean: ## Xóa bin/obj
	dotnet clean $(SLN)

rebuild: clean build ## Clean rồi build lại

test: ## Chạy test
	dotnet test $(SLN)

format: ## Format code
	dotnet format $(SLN)

# --- Database / EF Core ---
migrate: ## Tạo migration mới: make migrate name=TenMigration
	@if [ -z "$(name)" ]; then echo "Thieu ten. Dung: make migrate name=TenMigration"; exit 1; fi
	ConnectionStrings__DefaultConnection="$(CONN)" \
		dotnet ef migrations add $(name) --project $(DAL) --startup-project $(DAL)

db-update: ## Áp dụng migration vào DB
	ConnectionStrings__DefaultConnection="$(CONN)" \
		dotnet ef database update --project $(DAL) --startup-project $(DAL)

db-drop: ## Xóa database
	ConnectionStrings__DefaultConnection="$(CONN)" \
		dotnet ef database drop --force --project $(DAL) --startup-project $(DAL)

secrets: ## Tạo user-secrets mặc định (sửa key thật trước khi chạy)
	dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.local" --project $(WEB)
	dotnet user-secrets set "BootstrapAdmin:FullName" "System Admin" --project $(WEB)
	dotnet user-secrets set "BootstrapAdmin:Password" "ChangeMe123" --project $(WEB)
	dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY" --project $(WEB)
	dotnet user-secrets set "HuggingFace:ApiKey" "YOUR_HUGGINGFACE_KEY" --project $(WEB)
