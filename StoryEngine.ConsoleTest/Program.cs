using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryEngine.Models;
using StoryEngine.Engine;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== START TEST MOTOR STORY ENGINE ===");

        // 1. Calea către fișierul JSON de test (asigură-te că fișierul story_sample.json e în același folder cu executabilul sau dă-i cale absolută)
        string jsonPath = "story_sample.json";

        if (!File.Exists(jsonPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[EROARE] Nu am găsit fișierul '{jsonPath}'!");
            Console.WriteLine("Creează fișierul lângă Program.cs și copiază textul din story_sample.json în el.");
            Console.ResetColor();
            return;
        }

        try
        {
            // 2. Încărcăm povestea direct din JSON brut (pentru a testa pur doar motorul)
            string jsonContent = File.ReadAllText(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };

            StoryDefinition story = JsonSerializer.Deserialize<StoryDefinition>(jsonContent, options);
            Console.WriteLine($"[SUCCES] Povestea '{story.Title}' a fost încărcată corect!\n");

            // 3. Inițializăm Motorul Jocului
            GameEngine engine = new GameEngine(story);
            engine.StartNewGame();

            // 4. Bucla principală de text a jocului
            while (!engine.State.IsGameOver)
            {
                StoryBlock currentBlock = engine.GetCurrentBlock();

                // Afișăm HUD-ul cu resurse
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"--------------------------------------------------");
                Console.WriteLine($"[ZIUA {engine.State.Day}] | " +
                                  $"Food: {engine.State.Properties["food"]} | " +
                                  $"Water: {engine.State.Properties["water"]} | " +
                                  $"Health: {engine.State.Properties["health"]} | " +
                                  $"Morale: {engine.State.Properties["morale"]}");
                Console.WriteLine($"--------------------------------------------------");
                Console.ResetColor();

                // Afișăm textul poveștii
                Console.WriteLine(currentBlock.Text);
                Console.WriteLine();

                // Luăm deciziile valide filtrate prin condiții
                var availableDecisions = engine.GetAvailableDecisions();

                if (availableDecisions.Count == 0 && !currentBlock.IsFinal)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[BLOCAJ] Acest nod nu are decizii valide disponibile, dar nu este marcat ca Final!");
                    Console.ResetColor();
                    break;
                }

                // Afișăm opțiunile utilizatorului
                for (int i = 0; i < availableDecisions.Count; i++)
                {
                    Console.WriteLine($" [{i}] {availableDecisions[i].Text}");
                }

                // Citim alegerea jucătorului
                int indexAlegere = -1;
                while (indexAlegere < 0 || indexAlegere >= availableDecisions.Count)
                {
                    Console.Write("\nAlege o opțiune (introdu numărul): ");
                    string input = Console.ReadLine();
                    if (!int.TryParse(input, out indexAlegere) || indexAlegere < 0 || indexAlegere >= availableDecisions.Count)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Opțiune invalidă! Încearcă din nou.");
                        Console.ResetColor();
                    }
                }

                // Aplicăm decizia și trecem la următorul bloc
                engine.ChooseDecision(availableDecisions[indexAlegere]);
            }

            // Afișăm deznodământul (Finalul jocului)
            StoryBlock finalBlock = engine.GetCurrentBlock();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n==================== GAME OVER ====================");
            Console.WriteLine(finalBlock.Text);
            Console.WriteLine("===================================================");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[CRASH] A apărut o eroare la rulare: {ex.Message}");
            Console.ResetColor();
        }

        Console.ReadLine(); // Ține consola deschisă la final
    }
}