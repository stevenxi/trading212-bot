using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ark_tracker.app
{
    class Trading212Handler : IDisposable
    {
        private readonly ChromeDriver _driver;
        public Trading212Handler()
        {
            _driver = new ChromeDriver();
        }

        public void Start()
        {
            _driver.Navigate().GoToUrl(@"https://live.trading212.com/beta");
        }


        public void StartEdit()
        {
            //.portfolio-icon

            var container = _driver.FindElement(By.ClassName("bucket-instruments-personalisation"))
                .FindElement(By.ClassName("scrollable-items"));

            var existingInstruments = LoadSelected();

            var updateList = GetUpdateList();

            var newTickers = new HashSet<string>(updateList.Select(x => x.Ticker).Except(existingInstruments.Select(x => x.Ticker), StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase);
            var removeTickers = new HashSet<string>(existingInstruments.Select(x => x.Ticker).Except(updateList.Select(x => x.Ticker), StringComparer.InvariantCultureIgnoreCase), StringComparer.InvariantCultureIgnoreCase);


            if (updateList.Any())
            {
                _driver.FindElement(By.ClassName("bucket-customisation-footer")).FindElement(By.ClassName("add-slice-button")).Click();

                Thread.Sleep(1000);

                foreach (var instr in updateList)
                {
                    if (!newTickers.Contains(instr.Ticker))
                        continue;
                    var searchInput = _driver.FindElement(By.ClassName("bucket-add-slices")).FindElement(By.ClassName("search-input"))
                        .FindElement(By.TagName("input"));

                    searchInput.Clear();

                    searchInput.SendKeys(instr.Ticker);

                    searchInput.SendKeys("\n");

                    Thread.Sleep(1500);

                    var found = _driver.FindElement(By.ClassName("bucket-add-slices")).FindElement(By.ClassName("search-results-content"))
                        .FindElements(By.ClassName("item-wrapper"));

                    if (found.Count != 1)
                        continue;

                    found.First().FindElement(By.ClassName("add-to-bucket")).Click();

                    Thread.Sleep(500);
                }

                _driver.FindElement(By.ClassName("bucket-add-slices-footer-wrapper")).FindElement(By.ClassName("accent-button")).Click();
            }

            if (removeTickers.Any())
            {
                foreach(var instr in existingInstruments)
                {
                    if (!removeTickers.Contains(instr.Ticker))
                        continue;

                    instr.BinButton.Click();
                    Thread.Sleep(500);
                }
                Thread.Sleep(1000);
            }

            Thread.Sleep(1000);

            existingInstruments = LoadSelected();

            foreach (var pair in updateList.Join(existingInstruments, u =>u.Ticker, e=>e.Ticker, (u,e) => new { u, e }, StringComparer.InvariantCultureIgnoreCase))
            {
                pair.e.TargetInput.Clear();
                pair.e.TargetInput.SendKeys(pair.u.Percentage.ToString());
            }

            var missing = updateList.Select(x => x.Ticker).Except(existingInstruments.Select(x => x.Ticker), StringComparer.InvariantCultureIgnoreCase);

            Console.WriteLine($"Missing: {string.Join(",", missing)}");
        }

        private class InstrumentModel
        {
            [JsonProperty("date")]
            public DateTime Date { get; set; }
            [JsonProperty("ticker")]
            public string Ticker { get; set; }
            [JsonProperty("percent")]
            public decimal Percentage { get; set; }
        }

        private class ExistingInstrument
        {
            public string Ticker { get; set; }
            public IWebElement TargetInput { get; set; }
            public IWebElement BinButton { get; set; }
        }

        private List<ExistingInstrument> LoadSelected()
        {
            var container = _driver.FindElement(By.ClassName("bucket-instruments-personalisation"))
                .FindElement(By.ClassName("scrollable-items"));

            var existingInstruments = container.FindElements(By.ClassName("bucket-instrument-personalisation")).Select(x =>
            {
                var targetInput = x.FindElement(By.ClassName("formatted-number-input")).FindElement(By.TagName("input"));
                var id = x.GetAttribute("id");
                var targetPercentTxt = targetInput.GetAttribute("value");

                return new ExistingInstrument {Ticker = id.Split('_')[0], TargetInput = targetInput, BinButton = x.FindElement(By.ClassName("close-button"))};
            }).ToList();

            return existingInstruments;
        }


        private List<InstrumentModel> GetUpdateList()
        {
            string json;
            using (var webClient = new System.Net.WebClient())
            {
                json = webClient.DownloadString($"https://www.arktrack.com/ARKK.json?{DateTime.Today: yyyyMMdd}0");
            }

            var instruments = JsonConvert.DeserializeObject<List<InstrumentModel>>(json);

            var latestDate = instruments.OrderByDescending(x => x.Date).First().Date;

            return instruments.Where(x => x.Date == latestDate).ToList();
        }




        public void Dispose()
        {
            _driver.Dispose();
        }
    }
}
