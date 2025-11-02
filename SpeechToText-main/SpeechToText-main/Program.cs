using System;
using System.IO;
using System.Text.Json; // JSON işleme için
using NAudio.Wave;
using Vosk;

namespace VoskMicDemo
{
    class Program
    {
        // JSON'dan metni ayıklamak için yardımcı fonksiyon (değişiklik yok)
        static string GetTextFromJson(string json, string key)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    if (doc.RootElement.TryGetProperty(key, out JsonElement textElement))
                    {
                        return textElement.GetString() ?? string.Empty;
                    }
                }
            }
            catch (JsonException)
            {
                return string.Empty;
            }
            return string.Empty;
        }

        static void Main(string[] args)
        {
            // 1. Vosk Modelini Yükle (değişiklik yok)
            Vosk.Vosk.SetLogLevel(0);
            string modelPath = "model-tr";

            if (!Directory.Exists(modelPath))
            {
                Console.WriteLine($"HATA: Model klasörü bulunamadı!");
                Console.WriteLine($"Lütfen '{modelPath}' klasörünü programın çalıştığı dizine kopyalayın.");
                Console.WriteLine($"({Path.GetFullPath(".")})");
                Console.ReadKey();
                return;
            }

            using (Model model = new Model(modelPath))
            {
                // 2. Vosk Tanıyıcıyı Oluştur (değişiklik yok)
                using (VoskRecognizer recognizer = new VoskRecognizer(model, 16000.0f))
                {
                    recognizer.SetMaxAlternatives(0);
                    recognizer.SetWords(true);

                    // 3. NAudio ile Mikrofonu Ayarla (değişiklik yok)
                    using (WaveInEvent waveIn = new WaveInEvent())
                    {
                        waveIn.DeviceNumber = 0;
                        waveIn.WaveFormat = new WaveFormat(16000, 16, 1);

                        // === DEĞİŞİKLİK BURADA BAŞLIYOR ===

                        // Mikrofondan ses verisi geldiğinde bu event (olay) tetiklenir
                        waveIn.DataAvailable += (sender, e) =>
                        {
                            // Sesi yakala ve anında Vosk'a besle
                            // 'if' kontrolünü kaldırdık, çünkü artık 'Result' kullanmıyoruz.
                            recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded);

                            // Sadece kısmi sonucu (PartialResult) alıp gösteriyoruz.
                            // Bu, konuşma devam ettikçe sürekli güncellenecek.
                            string partialText = GetTextFromJson(recognizer.PartialResult(), "partial");

                            // Eğer kısmi metin boş değilse, aynı satıra yazdır (\r ile)
                            if (!string.IsNullOrEmpty(partialText))
                            {
                                Console.Write($"[Konuşuluyor...]: {partialText}\r");
                            }
                        };

                        // === DEĞİŞİKLİK BURADA BİTİYOR ===

                        // 4. Dinlemeyi Başlat (değişiklik yok)
                        waveIn.StartRecording();
                        Console.WriteLine("Dinliyorum... Konuşmaya başlayın.");
                        Console.WriteLine("(Durdurmak ve son sonucu görmek için Enter'a basın)");
                        Console.ReadLine();

                        // 5. Durdur ve Temizle
                        waveIn.StopRecording();

                        // Kayıt durduktan sonra, bellekte kalan tüm sesi işle
                        // ve 'FinalResult' ile TAM ve TEK bir sonuç al.
                        string finalText = GetTextFromJson(recognizer.FinalResult(), "text");

                        // Önceki satırdaki "[Konuşuluyor...]" yazısını temizlemek için
                        // boşluklarla dolu bir satır yazıyoruz.
                        Console.Write(new string(' ', Console.WindowWidth - 1) + "\r");

                        // Şimdi son metni temiz bir şekilde yazdırıyoruz
                        Console.WriteLine($"\n[Son Metin]: {finalText}");
                    }
                }
            }
        }
    }
}