using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Models;
using NoteTrackerApp.Helpers;

// Run as a standalone script: dotnet run --project . -- seed-students
// Or call from Program.cs startup

namespace NoteTrackerApp.Seeds
{
    public static class StudentSeeder
    {
        private static readonly string[] FirstNames = [
            "Ahmet", "Mehmet", "Ali", "Hasan", "Hüseyin", "İbrahim", "Mustafa", "Ömer", "Murat", "Emre",
            "Can", "Burak", "Onur", "Serkan", "Furkan", "Enes", "Berk", "Kaan", "Tolga", "Oğuz",
            "Fatma", "Ayşe", "Emine", "Hatice", "Zeynep", "Merve", "Selin", "Elif", "Büşra", "Dilan",
            "Ece", "Gül", "Nur", "Ceren", "Tuğba", "Pınar", "Aslı", "Derya", "Esra", "Neslihan",
            "Yusuf", "Kerem", "Umut", "Eren", "Selim", "Erkan", "Doğan", "Barış", "Alper", "Volkan",
            "Sena", "İrem", "Gizem", "Şeyma", "Buse", "Hande", "Özge", "Arzu", "Sibel", "Gamze",
            "Ramazan", "Bayram", "Yasin", "Hamza", "Bilal", "Salih", "Hakan", "Orhan", "Fikret", "Cengiz"
        ];

        private static readonly string[] LastNames = [
            "Yılmaz", "Kaya", "Demir", "Çelik", "Şahin", "Doğan", "Kılıç", "Arslan", "Koç", "Kurt",
            "Aydın", "Özkan", "Çetin", "Yıldız", "Erdoğan", "Aktaş", "Polat", "Bozkurt", "Güler", "Coşkun",
            "Korkmaz", "Öztürk", "Yıldırım", "Güneş", "Aslan", "Taş", "Tekin", "Duman", "Kaplan", "Bulut",
            "Şimşek", "Acar", "Turan", "Aksoy", "Ateş", "Erdem", "Karaca", "Güven", "Sarı", "Bayrak"
        ];

        private static readonly string[] Classes = ["9-A", "9-B", "10-A", "10-B", "11-A", "11-B", "12-A", "12-B"];

        public static async Task SeedAsync(AppDbContext context)
        {
            // Mevcut öğrenci sayısını kontrol et
            var existingStudentCount = await context.Students.CountAsync();
            if (existingStudentCount >= 100)
            {
                Console.WriteLine($"[Seeder] Zaten {existingStudentCount} öğrenci var. Atlıyor.");
                return;
            }

            var rng = new Random(42); // Sabit seed = tekrarlanabilir
            int userIdCounter = await context.Users.MaxAsync(u => u.Id) + 1;
            int studentIdCounter = existingStudentCount > 0 
                ? (await context.Students.MaxAsync(s => s.Id) + 1) 
                : 1;
            
            var existingUsernames = (await context.Users.Select(u => u.Username).ToListAsync()).ToHashSet();
            var existingNumbers = (await context.Students.Select(s => s.StudentNumber).ToListAsync()).ToHashSet();

            var usersToAdd = new List<AppUser>();
            var studentsToAdd = new List<Student>();

            int added = 0;
            int attempt = 0;

            while (added < 100 && attempt < 5000)
            {
                attempt++;
                var firstName = FirstNames[rng.Next(FirstNames.Length)];
                var lastName = LastNames[rng.Next(LastNames.Length)];
                var fullName = $"{firstName} {lastName}";
                
                // Benzersiz kullanıcı adı oluştur
                var username = $"{firstName.ToLower().Replace("ı","i").Replace("ğ","g").Replace("ş","s").Replace("ç","c").Replace("ö","o").Replace("ü","u")}{rng.Next(100, 999)}";
                
                if (existingUsernames.Contains(username)) continue;
                
                var studentNumber = $"2024{(studentIdCounter + 1000):D4}";
                if (existingNumbers.Contains(studentNumber)) continue;

                existingUsernames.Add(username);
                existingNumbers.Add(studentNumber);

                var user = new AppUser
                {
                    Id = userIdCounter++,
                    Username = username,
                    FullName = fullName,
                    Role = "Student",
                    CanChangePhoto = rng.Next(2) == 1
                };
                user.Password = PasswordHelper.HashPassword(user, "okul1234");

                var student = new Student
                {
                    Id = studentIdCounter++,
                    UserId = user.Id,
                    StudentNumber = studentNumber,
                    ClassSection = Classes[rng.Next(Classes.Length)],
                    RegisteredAt = DateTime.Now.AddDays(-rng.Next(30, 365))
                };

                usersToAdd.Add(user);
                studentsToAdd.Add(student);
                added++;
            }

            context.Users.AddRange(usersToAdd);
            await context.SaveChangesAsync();

            context.Students.AddRange(studentsToAdd);
            await context.SaveChangesAsync();

            Console.WriteLine($"[Seeder] {added} öğrenci başarıyla eklendi.");
        }
    }
}
