using Lottery.Models;

namespace Lottery.ViewModels
{
    public partial class StudentVM : ContentView
    {
        StudentM student;
        public string Name { get => student.Name; set { student.Name = value; OnPropertyChanged(nameof(Name)); } }
        public int IdNr { get => student.Id; set { student.Id = value; OnPropertyChanged(nameof(IdNr)); } }
        public bool Present { get => student.Present; set { student.Present = value; OnPropertyChanged(nameof(Present)); } }

        public StudentVM(StudentM model)
        {
            student = model;
        }

    }
}
