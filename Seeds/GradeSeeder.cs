using Microsoft.EntityFrameworkCore;
using NoteTrackerApp.Data;
using NoteTrackerApp.Models;

namespace NoteTrackerApp.Seeds
{
    /// <summary>
    /// Mevcut öğrencilere Türkiye Yazılım Teknolojisi lisesi müfredatına
    /// uygun dersler ve rastgele notlar atar.
    /// </summary>
    public static class GradeSeeder
    {
        // Sınıfa göre ders listesi (Yazılım Teknolojisi Lisesi)
        private static readonly Dictionary<string, string[]> CoursesPerClass = new()
        {
            ["9"]  = ["Türkçe", "Matematik", "Fizik", "Kimya", "Biyoloji", "Tarih", "Coğrafya",
                      "İngilizce", "Beden Eğitimi", "Algoritmik Düşünce", "Programlama Temelleri",
                      "Din Kültürü ve Ahlak Bilgisi"],
            ["10"] = ["Türkçe", "Matematik", "Fizik", "Kimya", "Biyoloji", "Tarih", "Coğrafya",
                      "İngilizce", "Beden Eğitimi", "Nesne Tabanlı Programlama", "Web Tasarımı",
                      "Din Kültürü ve Ahlak Bilgisi"],
            ["11"] = ["Türkçe", "Matematik", "Fizik", "Kimya", "Tarih", "İngilizce",
                      "Beden Eğitimi", "Veritabanı Programlama", "Ağ Temelleri", "İşletim Sistemleri",
                      "Din Kültürü ve Ahlak Bilgisi"],
            ["12"] = ["Türkçe", "Matematik", "Tarih", "İngilizce", "Beden Eğitimi",
                      "Yazılım Geliştirme", "Proje Yönetimi", "Siber Güvenlik",
                      "Din Kültürü ve Ahlak Bilgisi", "İnkılap Tarihi ve Atatürkçülük"]
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            // Not zaten girilmişse atla
            if (await context.Grades.AnyAsync())
            {
                Console.WriteLine("[GradeSeeder] Notlar zaten mevcut. Atlıyor.");
                return;
            }

            var students = await context.Students.Include(s => s.User).ToListAsync();
            if (!students.Any())
            {
                Console.WriteLine("[GradeSeeder] Öğrenci bulunamadı.");
                return;
            }

            var rng = new Random(99);
            var gradesToAdd = new List<Grade>();

            foreach (var student in students)
            {
                // Sınıf numarasını al (9-A → "9", 10-B → "10" vs.)
                var classNum = student.ClassSection?.Split('-')[0] ?? "9";
                if (!CoursesPerClass.TryGetValue(classNum, out var courses))
                    courses = CoursesPerClass["9"];

                foreach (var course in courses)
                {
                    foreach (var semester in new[] { "1. Dönem", "2. Dönem" })
                    {
                        var grade = new Grade
                        {
                            StudentId = student.Id,
                            CourseName = course,
                            Semester = semester,
                            Exam1 = RandomGrade(rng, course),
                            Exam2 = RandomGrade(rng, course),
                            Oral1 = RandomOral(rng),
                            Oral2 = RandomOral(rng),
                            Performance1 = RandomPerf(rng),
                            Performance2 = RandomPerf(rng),
                            Project = rng.Next(0, 4) == 0 ? null : RandomProject(rng), // %25 olasılıkla proje yok
                            CreatedAt = DateTime.Now.AddDays(-rng.Next(0, 60))
                        };
                        grade.CalculateAverage();
                        gradesToAdd.Add(grade);
                    }
                }
            }

            // Toplu ekle (performans için 500'lük gruplar)
            const int batchSize = 500;
            for (int i = 0; i < gradesToAdd.Count; i += batchSize)
            {
                var batch = gradesToAdd.Skip(i).Take(batchSize).ToList();
                context.Grades.AddRange(batch);
                await context.SaveChangesAsync();
            }

            Console.WriteLine($"[GradeSeeder] {gradesToAdd.Count} not kaydı oluşturuldu.");
        }

        // Beden Eğitimi ve Programlama derslerinde genelde daha yüksek not
        private static decimal RandomGrade(Random rng, string course)
        {
            int min = course is "Matematik" or "Fizik" or "Kimya" ? 40 : 50;
            int max = course is "Beden Eğitimi" or "Din Kültürü ve Ahlak Bilgisi" ? 80 : 70;
            return rng.Next(min, 101);
        }

        private static decimal RandomOral(Random rng) => rng.Next(55, 101);
        private static decimal RandomPerf(Random rng) => rng.Next(60, 101);
        private static decimal RandomProject(Random rng) => rng.Next(65, 101);
    }
}
