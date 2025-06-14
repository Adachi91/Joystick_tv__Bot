using System;
//using System.Collections.Generic;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;

namespace ShimamuraBot.Modules
{
    class Viewer
    {
        public int Points { get; set; }
        public string Name { get; set; }
    }

    class ViewerList {
        private List<Viewer> _viewers = new List<Viewer>();

        public void RetroAdd(string path) {
            if (!File.Exists(path)) return;

            Dictionary<string, Tuple<DateTime, DateTime>> fifo_fucker = new();
            Dictionary<string, int> total = new();

            string[] rawlog = File.ReadAllLines(path);

            foreach (string line in rawlog) {
                if (line.Contains("entered chat")) {
                    var name = line.Substring(3/* Fix me*/, line.Length);
                    if (!fifo_fucker.ContainsKey(name)) {
                        if (!total.ContainsKey(name))
                            total.Add(name, 0);
                        fifo_fucker.Add(name, new Tuple<DateTime, DateTime>(DateTime.Now, DateTime.Now));
                    } else {
                        // parse logic.
                    }
                }
            }
        }

        public void Add(Viewer viewer) {
            _viewers.Add(viewer);
        }

        public void Remove(Viewer viewer) {
            _viewers.Remove(viewer);
        }

        public void Remove(string name) {
            _viewers.RemoveAll(v => v.Name == name);
        }

        public Viewer GetViewerByName(string name) {
            return _viewers.FirstOrDefault(v => v.Name == name);
        }

        public void UpdatePoints(string name, int points) {
            var viewer = GetViewerByName(name);
            if (viewer != null) {
                viewer.Points = points;
            }
        }

        public void SaveToFile(string path) {
            var sb = new StringBuilder();
            //  { { "Name": "Apple", "Points": 123 }, { "Name": "Banana", "Points": 456 } }
            foreach (var viewer in _viewers) {
                sb.Append(viewer.Stringify());
            }

            File.WriteAllText(path, sb.ToString());
        }

        public List<Viewer> GetAll() {
            return _viewers;
        }
    }
}
