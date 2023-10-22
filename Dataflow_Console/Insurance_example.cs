using System.Threading.Tasks.Dataflow;

namespace Dataflow_Console
{
    internal class Insurance_example
    {
        internal static void Run()
        {
            // Define blocks
            var days = new TransformBlock<int, (int year, int day)[]>(year =>
            {
                return Enumerable.Range(1, 365).Select(day => (year, day)).ToArray();
            });

            var earthquake = new TransformBlock<(int year, int day)[], YeltRow[]>(input =>
            {
                var rand = new Random();
                var yearProbability = 0.1;
                var dayProbability = yearProbability / 365;
                var exposure = 1000000m;
                const int eventId = 1;

                var yelt = new List<YeltRow>();
                foreach (var (year, day) in input)
                {
                    var happenedThisDay = rand.NextDouble() < dayProbability;
                    var severity = rand.NextDouble();
                    var loss = happenedThisDay ? (decimal)severity * exposure : 0;

                    yelt.Add(new YeltRow(year, eventId, loss));
                }

                return yelt.ToArray();
            });

            var sum = new TransformBlock<YeltRow[], (int year, decimal sum)>(yeltRows =>
            {
                var year = yeltRows[0].Year;
                if (yeltRows.Any(row => row.Year != year)) throw new ArgumentException("YELT is not consistent");
                var sum = yeltRows.Sum(row => row.Loss);
                return (year, sum);
            });

            var filter = new TransformBlock<(int year, decimal sum), object?>(pair =>
            {
                return pair.sum == 0 ? null : pair;
            });

            var print = new ActionBlock<object?>(obj =>
            {
                if (obj == null) return;
                Console.WriteLine(obj.ToString());
            });

            // Link blocks in to a pipeline
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            days.LinkTo(earthquake, linkOptions);
            earthquake.LinkTo(sum, linkOptions);
            sum.LinkTo(filter, linkOptions);
            filter.LinkTo(print, linkOptions);

            // Push data to pipeline and start it
            const int trials = 100;
            Enumerable.Range(1, trials).ToList().ForEach(year => days.Post(year));
            days.Complete();

            // Wait for completion
            print.Completion.Wait();

            Console.WriteLine("Done! Press any key");
            Console.ReadKey();
        }
    }

    internal record YeltRow(int Year, int EventID, decimal Loss);
}
