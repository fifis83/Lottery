using Lottery.ViewModels;

namespace Lottery.Models
{
    internal class ClassM
    {
        public string Name { get; set; }
        public List<StudentVM> Students;
        public LinkedList<StudentVM> LastDrawnStudents = new LinkedList<StudentVM>();

        public ClassM(string name, List<StudentVM> students)
        {
            Name = name;
            Students = students;
        }

        public ClassM(string name)
        {
            Name = name;
            Students = new List<StudentVM>();
        }

        public void SortStudents()
        {
            Students = Students.OrderBy(s=>s.Name).ToList();

            for (int i = 0; i < Students.Count; i++)
            {
                Students[i].IdNr = i+1;
            }
        }
    }
}
