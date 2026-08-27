# TaskFlow

TaskFlow; görevlerinizi planlamanızı, önceliklendirmenizi, filtrelemenizi ve yaklaşan son tarihler için e-posta hatırlatıcıları oluşturmanızı sağlayan full-stack bir görev yönetimi uygulamasıdır.

## Özellikler

- JWT tabanlı kayıt ve giriş
- Kullanıcıya özel görev izolasyonu
- Görev oluşturma, düzenleme, silme ve durum güncelleme
- Arama, filtreleme, sıralama ve sayfalama
- Kullanıcıya özel sayfa boyutu ve sıralama tercihleri
- Gmail SMTP üzerinden zamanlanmış e-posta hatırlatıcıları
- Merkezi hata yönetimi ve Serilog loglama
- EF Core migration desteği
- Docker Compose ile API ve SQL Server kurulumu
- xUnit tabanlı servis testleri

## Teknolojiler

| Katman | Teknolojiler |
| --- | --- |
| Backend | ASP.NET Core 10, C#, Entity Framework Core |
| Veritabanı | Microsoft SQL Server |
| Kimlik doğrulama | JWT Bearer, BCrypt |
| Frontend | React 19, TypeScript, Vite |
| Test | xUnit, Moq, EF Core InMemory |
| Operasyon | Docker, Docker Compose, Serilog |

## Proje yapısı

```text
TaskFlow/
├── TaskFlow/             # ASP.NET Core Web API
├── TaskFlow.Tests/       # Backend birim testleri
├── taskflow-client/      # React uygulaması
├── compose.yaml          # API ve SQL Server servisleri
└── .env.example          # Docker yapılandırma şablonu
```

## Hızlı başlangıç — Docker

Gereksinimler: Docker Desktop ve Node.js.

```bash
cp .env.example .env
```

`.env` içindeki SQL Server parolası ile JWT anahtarını güçlü, benzersiz değerlerle değiştirin. Ardından API ve veritabanını başlatın:

```bash
docker compose up --build
```

Frontend'i ayrı bir terminalde çalıştırın:

```bash
cd taskflow-client
cp .env.example .env
npm ci
npm run dev
```

Web uygulaması varsayılan olarak `http://localhost:5173`, API ise `http://localhost:5070` adresinde açılır. Docker başlangıcında migration'lar otomatik uygulanır.

### Docker sorun giderme

- `sqlserver is unhealthy` ve `Login failed for user 'sa'` hataları birlikte görünüyorsa `.env` içindeki `MSSQL_SA_PASSWORD` daha önce oluşturulan Docker volume'undaki paroladan farklıdır. Verileri korumak için `.env` dosyasına ilk kurulumda kullandığınız parolayı geri yazın ve `docker compose up -d` çalıştırın.
- Yerel veriler önemli değilse temiz kurulum için `docker compose down -v` ve ardından `docker compose up --build` çalıştırın. `down -v` yerel TaskFlow veritabanını siler.
- Apple Silicon Mac'lerde SQL Server amd64 emülasyonu ile çalışır. Compose dosyası gerekli platformu açıkça seçer ve beklenmeyen bir container kapanmasında servisleri otomatik yeniden başlatır.
- Container durumlarını `docker compose ps`, son hata kayıtlarını `docker compose logs --tail=50` ile kontrol edebilirsiniz.

## Yerel geliştirme

Gereksinimler: .NET 10 SDK, Node.js ve çalışan bir SQL Server.

Gizli değerleri repoya yazmak yerine .NET User Secrets kullanın:

```bash
dotnet user-secrets set --project TaskFlow "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=TaskFlowDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
dotnet user-secrets set --project TaskFlow "Jwt:Key" "YOUR_RANDOM_SECRET_WITH_AT_LEAST_32_CHARACTERS"
```

İsteğe bağlı e-posta hatırlatıcıları için:

```bash
dotnet user-secrets set --project TaskFlow "Gmail:Address" "your-address@gmail.com"
dotnet user-secrets set --project TaskFlow "Gmail:AppPassword" "YOUR_GMAIL_APP_PASSWORD"
```

Veritabanını ve API'yi hazırlayın:

```bash
dotnet tool install --global dotnet-ef
dotnet ef database update --project TaskFlow
dotnet run --project TaskFlow
```

Frontend'i başlatın:

```bash
cd taskflow-client
cp .env.example .env
npm ci
npm run dev
```

## Test ve kalite kontrolleri

```bash
dotnet test TaskFlow.sln
cd taskflow-client
npm run lint
npm run build
```

## API özeti

| Yöntem | Endpoint | Açıklama |
| --- | --- | --- |
| `POST` | `/Auth/register` | Yeni kullanıcı oluşturur |
| `POST` | `/Auth/login` | JWT token üretir |
| `GET` | `/Task` | Görevleri filtreli ve sayfalı getirir |
| `POST` | `/Task` | Görev oluşturur |
| `PUT` | `/Task/{id}` | Görevi günceller |
| `PATCH` | `/Task/{id}/status` | Görev durumunu günceller |
| `DELETE` | `/Task/{id}` | Görevi siler |
| `GET`, `PUT` | `/settings` | Kullanıcı tercihlerini yönetir |
| `GET`, `POST` | `/reminders` | E-posta hatırlatıcılarını yönetir |

Development ortamında OpenAPI belgesi `/openapi/v1.json` adresinden alınabilir. Hazır örnek istekler [`TaskFlow/TaskFlow.http`](TaskFlow/TaskFlow.http) dosyasındadır.

## Güvenlik notları

- Secret, parola ve gerçek token değerlerini repoya eklemeyin.
- Üretimde `Cors:AllowedOrigins` değerini gerçek frontend adresinizle yapılandırın.
- Üretim JWT anahtarını güvenli bir secret manager üzerinden sağlayın.
- Gmail entegrasyonu için normal hesap parolası yerine uygulama parolası kullanın.

## Gelecek geliştirmeler

- Controller ve entegrasyon testi kapsamını genişletme
- Refresh token ve parola yenileme akışı
- Hatırlatıcı düzenleme/silme
- CI pipeline ve otomatik deployment
- Mobil uyumluluk ve erişilebilirlik iyileştirmeleri
