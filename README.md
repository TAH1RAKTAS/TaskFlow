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
| Veritabanı | SQLite (hızlı başlangıç), Microsoft SQL Server (Docker/kalıcı geliştirme) |
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

## En hızlı başlangıç — `dotnet run`

Gereksinimler: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) ve frontend için Node.js.

Projeyi klonlayıp backend klasörüne girin:

```bash
git clone https://github.com/TAH1RAKTAS/TaskFlow.git
cd TaskFlow/TaskFlow
dotnet run
```

İlk çalıştırmada paketler ve yerel SQLite veritabanı otomatik hazırlanır. Başka bir
veritabanı, parola, `.env` veya Docker kurulumu gerekmez. Backend çalıştığında:

- API durum sayfası: `http://localhost:5070`
- OpenAPI belgesi: `http://localhost:5070/openapi/v1.json`

Frontend'i ikinci bir terminalde başlatın:

```bash
cd TaskFlow/taskflow-client
npm install
npm run dev
```

Tarayıcıdan `http://localhost:5173` adresine gidin. İlk kullanımda **Kayıt Ol**
ekranından hesabınızı oluşturup ardından giriş yapın.

> Yerel SQLite veritabanı `taskflow.development.db` adıyla oluşur ve Git'e eklenmez.
> Proje kökünde geçerli bir `.env` varsa uygulama mevcut SQL Server geliştirme
> veritabanını kullanır; böylece daha önce oluşturulmuş kullanıcı ve görevler korunur.

## Docker ile SQL Server

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

## SQL Server ile yerel geliştirme

Bu bölüm yalnızca SQLite yerine SQL Server kullanmak isteyen geliştiriciler içindir.
Hızlı başlangıç için yukarıdaki `dotnet run` adımları yeterlidir.

Gizli değerleri repoya yazmak yerine .NET User Secrets kullanın:

```bash
dotnet user-secrets set --project TaskFlow "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=TaskFlowDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
dotnet user-secrets set --project TaskFlow "Jwt:Key" "YOUR_RANDOM_SECRET_WITH_AT_LEAST_32_CHARACTERS"
dotnet user-secrets set --project TaskFlow "Database:Provider" "SqlServer"
dotnet user-secrets set --project TaskFlow "Database:EnsureCreated" "false"
dotnet user-secrets set --project TaskFlow "Database:ApplyMigrations" "true"
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
