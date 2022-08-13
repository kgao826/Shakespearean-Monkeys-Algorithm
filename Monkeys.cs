namespace Monkeys {
    using Carter;
    using Carter.ModelBinding;
    using Carter.Request;
    using Carter.Response;
    using Microsoft.AspNetCore.Http;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using static System.Console;
    
    public class HomeModule : CarterModule {
        public HomeModule () {
            Post ("/try", async (req, res) => {
                var clientGenome = await req.Bind<TryRequest> ();
                if (clientGenome.length != 0){
                    GeneticAlgorithm(clientGenome);
                }
                else{
                    await Task.Delay (0);
                    List<String> gen_genome = new List<String>();
                    gen_genome.Add("");
                    AssessRequest obj1 = new AssessRequest {id=clientGenome.id, genomes=gen_genome};
                    AssessResponse ares = await PostFitnessAssess(obj1);
                    var genomeLength = ares.scores;
                    clientGenome.length = genomeLength[0];
                    GeneticAlgorithm(clientGenome);
                }
                WriteLine ($".... POST /try {clientGenome}");
                await Task.Delay (0);
                return;
            });
        }
        
        async Task<AssessResponse> PostFitnessAssess (AssessRequest areq) {
            var client = new HttpClient ();                
            client.BaseAddress = new Uri ("http://localhost:8091/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (
                new MediaTypeWithQualityHeaderValue ("application/json")
            );    
            var hrm = await client.PostAsJsonAsync ("/assess", areq);
            hrm.EnsureSuccessStatusCode ();
            await Task.Delay (0);
            var ares = new AssessResponse ();
            ares = await hrm.Content.ReadAsAsync <AssessResponse> ();
            return ares;
        }
        
        async Task PostClientTop (TopRequest treq) {
            var client = new HttpClient ();
            var clientId = treq.id;
            client.BaseAddress = new Uri ("http://localhost:" + clientId + "/");
            client.DefaultRequestHeaders.Accept.Clear ();
            client.DefaultRequestHeaders.Accept.Add (
                new MediaTypeWithQualityHeaderValue ("application/json")
            );    
            var hrm = await client.PostAsJsonAsync ("/top", treq);
            hrm.EnsureSuccessStatusCode ();
            await Task.Delay (0);
            return;
        }
        
        private Random _random = new Random (1);
        
        private double NextDouble () {
            lock (this) {
                return _random.NextDouble ();
            }
        }
        
        private int NextInt (int a, int b) {
            lock (this) {
                return _random.Next (a, b);
            }
        }

        int ProportionalRandom (int[] weights, int sum) {
            var val = NextDouble () * sum;
            
            for (var i = 0; i < weights.Length; i ++) {
                if (val < weights[i]) return i;
                
                val -= weights[i];
            }
            
            WriteLine ($"***** Unexpected ProportionalRandom Error");
            return 0;
        }

        async void GeneticAlgorithm (TryRequest treq) {
            WriteLine ($"..... GeneticAlgorithm {treq}");
            await Task.Delay (0);
            
            var gen_id = treq.id;
            var monkeys = treq.monkeys;
            if (monkeys % 2 != 0) monkeys += 1;
            var length = treq.length;
            var crossover = treq.crossover / 100.0 ;
            var mutation = treq.mutation / 100.0;
            var limit = treq.limit;
            if (limit == 0) limit = 1000;
            var topscore = int.MaxValue;
            
            List<String> gen_genome = new List<String>();
            for (int i = 0; i < monkeys; i++){
                var gen_text = "";
                for (int j = 0; j < length; j++){
                    var c = (char) NextInt (32, 127);
                    gen_text += Convert.ToChar(c);
                }
                gen_genome.Add(gen_text);
            }

            AssessRequest obj1 = new AssessRequest {id=gen_id, genomes=gen_genome};
            for (int loop = 0; loop < limit; loop ++) {
                AssessResponse ares = await PostFitnessAssess(obj1);
                var resId = ares.id;
                var resScore = ares.scores;
                var bestScore = resScore.Min();
                gen_genome = obj1.genomes;
                var bestGenome = gen_genome[resScore.IndexOf(bestScore)];
                if (bestScore < topscore){
                    topscore = bestScore;
                    TopRequest obj2 = new TopRequest {id=resId, loop=loop, score=bestScore, genome=bestGenome};
                    await PostClientTop(obj2);
                }
                if(bestScore == 0){
                    break;
                } 
                var largest_hamming = resScore.Max();
                var weights = resScore.Select(n => largest_hamming - n + 1);
                var sumofweights = weights.Sum();

                var para = treq.parallel;

                if(para){
                    var new_genomes = ParallelEnumerable.Range (1, monkeys/2).SelectMany<int, string> (i => {
                        var index1 = ProportionalRandom(weights.ToArray(), sumofweights);
                        var index2 = ProportionalRandom(weights.ToArray(), sumofweights);
                        var p1 = gen_genome[index1];
                        var p2 = gen_genome[index2];
                        var c1 = "";
                        var c2 = "";
                        if (NextDouble() < crossover){
                            var crossIndex = NextInt(0, length);
                            c1 = p1.Substring(0, crossIndex) + p2.Substring(crossIndex);
                            c2 = p2.Substring(0, crossIndex) + p1.Substring(crossIndex);
                        }
                        else{
                            c1 = p1;
                            c2 = p2;
                        }
                        
                        var letter = 0;
                        var newString = "";
                        var charIndex = 0;
                        if  (NextDouble() < mutation){
                            charIndex = NextInt(0, length);
                            letter = (char) NextInt (32, 127);
                            newString = c1.Substring(0, charIndex) + Convert.ToChar(letter) + c1.Substring(charIndex + 1);
                            c1 = newString;
                        }
                        if  (NextDouble() < mutation){
                            charIndex = NextInt(0, length);
                            letter = (char) NextInt (32, 127);
                            newString = c2.Substring(0, charIndex) + Convert.ToChar(letter) + c2.Substring(charIndex + 1);
                            c2 = newString;
                        }
                        return new [] {c1, c2};
                    }).ToList();
                    obj1 = new AssessRequest {id=gen_id, genomes=new_genomes};
                }
                else{
                    var new_genomes = Enumerable.Range (1, monkeys/2).SelectMany<int, string> (i => {
                        var index1 = ProportionalRandom(weights.ToArray(), sumofweights);
                        var index2 = ProportionalRandom(weights.ToArray(), sumofweights);
                        var p1 = gen_genome[index1];
                        var p2 = gen_genome[index2];
                        var c1 = "";
                        var c2 = "";
                        if (NextDouble() < crossover){
                            var crossIndex = NextInt(0, length);
                            c1 = p1.Substring(0, crossIndex) + p2.Substring(crossIndex);
                            c2 = p2.Substring(0, crossIndex) + p1.Substring(crossIndex);
                        }
                        else{
                            c1 = p1;
                            c2 = p2;
                        }
                        
                        var letter = 0;
                        var newString = "";
                        var charIndex = 0;
                        if  (NextDouble() < mutation){
                            charIndex = NextInt(0, length);
                            letter = (char) NextInt (32, 127);
                            newString = c1.Substring(0, charIndex) + Convert.ToChar(letter) + c1.Substring(charIndex + 1);
                            c1 = newString;
                        }
                        if  (NextDouble() < mutation){
                            charIndex = NextInt(0, length);
                            letter = (char) NextInt (32, 127);
                            newString = c2.Substring(0, charIndex) + Convert.ToChar(letter) + c2.Substring(charIndex + 1);
                            c2 = newString;
                        }
                        return new [] {c1, c2};
                    }).ToList();
                    obj1 = new AssessRequest {id=gen_id, genomes=new_genomes};
                }
            }
        }
    }
    
    // public class TargetRequest {
        // public int id { get; set; }
        // public bool parallel { get; set; }
        // public string target { get; set; }
        // public override string ToString () {
            // return $"{{{id}, {parallel}, \"{target}\"}}";
        // }  
    // }    

    public class TryRequest {
        public int id { get; set; }
        public bool parallel { get; set; }
        public int monkeys { get; set; }
        public int length { get; set; }
        public int crossover { get; set; }
        public int mutation { get; set; }
        public int limit { get; set; }
        public override string ToString () {
            return $"{{{id}, {parallel}, {monkeys}, {length}, {crossover}, {mutation}, {limit}}}";
        }
    }
    
    public class TopRequest {
        public int id { get; set; }
        public int loop { get; set; }
        public int score { get; set; }
        public string genome { get; set; }
        public override string ToString () {
            return $"{{{id}, {loop}, {score}, {genome}}}";
        }  
    }    
    
    public class AssessRequest {
        public int id { get; set; }
        public List<string> genomes { get; set; }
        public override string ToString () {
            return $"{{{id}, #{genomes.Count}}}";
        }  
    }
    
    public class AssessResponse {
        public int id { get; set; }
        public List<int> scores { get; set; }
        public override string ToString () {
            return $"{{{id}, #{scores.Count}}}";
        }  
    }   
}

namespace Monkeys {
    using Carter;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup {
        public void ConfigureServices (IServiceCollection services) {
            services.AddCarter ();
        }

        public void Configure (IApplicationBuilder app) {
            app.UseRouting ();
            app.UseEndpoints( builder => builder.MapCarter ());
        }
    }
}

namespace Monkeys {
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program {
        public static void Main (string[] args) {
//          var host = Host.CreateDefaultBuilder (args)
//              .ConfigureWebHostDefaults (webBuilder => webBuilder.UseStartup<Startup>())

            var urls = new[] {"http://localhost:8081"};
            
            var host = Host.CreateDefaultBuilder (args)
            
                .ConfigureLogging (logging => {
                    logging
                        .ClearProviders ()
                        .AddConsole ()
                        .AddFilter (level => level >= LogLevel.Warning);
                })
                
                .ConfigureWebHostDefaults (webBuilder => {
                    webBuilder.UseStartup<Startup> ();
                    webBuilder.UseUrls (urls);  // !!!
                })
                
                .Build ();
            
            System.Console.WriteLine ($"..... starting on {string.Join (", ", urls)}");            
            host.Run ();
        }
    }
}

