using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using Lottery.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;

namespace Lottery.ViewModels
{
    public partial class MainPageVM : ContentPage
    {
        //TODO: add saving and loading

        public IList<string> PickerClassList { get; set; } = new ObservableCollection<string>() { "Nowa Klasa" };
        private List<ClassM> AllSchoolClasses;
        public ObservableCollection<StudentVM> CurrentClassStudents { get; set; }

        public string RandomStudent { get; set; } = string.Empty;
        public StudentVM SelectedStudent { get; set; }

        public bool AddClassVisible { get; set; } = false;
        public bool StudentsVisible { get; set; } = false;

        private int _luckyNr = 0;
        public string LuckyNr
        {
            get
            {
                if (_luckyNr == 0) return "Brak uczniów";
                return _luckyNr.ToString();
            }
        }
        private int _classIndex;
        public int ClassIndex
        {
            get => _classIndex;
            set
            {
                _classIndex = value;
                StudentsVisible = false;
                RandomStudent = string.Empty;

                if (value == PickerClassList.Count - 1)
                {
                    AddClassVisible = true;
                }
                else
                {
                    AddClassVisible = false;
                    if (value != -1)
                    {
                        setClassStudents(AllSchoolClasses[value]);
                        StudentsVisible = true;
                        if (AllSchoolClasses[value].LastDrawnStudents.Count != 0)
                            RandomStudent = AllSchoolClasses[value].LastDrawnStudents.Last.Value.Name;
                    }
                }

                OnPropertyChanged(nameof(AddClassVisible));
                OnPropertyChanged(nameof(ClassIndex));
                OnPropertyChanged(nameof(StudentsVisible));
                OnPropertyChanged(nameof(RandomStudent));
            }
        }

        public MainPageVM()
        {
            AllSchoolClasses = new List<ClassM>();
            CurrentClassStudents = new ObservableCollection<StudentVM>();

            //Load("C:\\Users\\filip\\Desktop\\text.txt");

            GenLuckyNr();

        }

        [RelayCommand]
        private void GenLuckyNr()
        {
            // IDK which is better
            Random rand = new Random();
            //Random rand = new Random(DateTime.Now.Day + DateTime.Now.Month + DateTime.Now.Year);

            int maxStudents = 0;
            foreach (var cls in AllSchoolClasses)
            {
                maxStudents = Math.Max(cls.Students.Count, maxStudents);
            }
            if (maxStudents == 0) _luckyNr = 0;
            else _luckyNr = rand.Next(1, maxStudents + 1);
            OnPropertyChanged(nameof(LuckyNr));
        }

        private void setClassStudents(ClassM StudentClass)
        {
            CurrentClassStudents.Clear();
            foreach (var item in StudentClass.Students)
            {
                CurrentClassStudents.Add(item);
            }
            OnPropertyChanged(nameof(CurrentClassStudents));
        }

        private List<StudentVM>? GetEligibleStudents(ClassM startingClass)
        {
            if (startingClass.Students.Count == 0) return null;
            var presentStudents = new List<StudentVM>();
            var eligibleStudents = new List<StudentVM>();
            var lastDrawnStudents = AllSchoolClasses[ClassIndex].LastDrawnStudents;

            foreach (var s in startingClass.Students)
            {
                if (s.Present && s.IdNr != _luckyNr) presentStudents.Add(s);
            }

            if (presentStudents.Count == 0) return null;

            if (lastDrawnStudents.Count > 3 || lastDrawnStudents.Count >= presentStudents.Count) lastDrawnStudents.RemoveFirst();

            foreach (var s in presentStudents)
            {
                if (!lastDrawnStudents.Contains(s)) eligibleStudents.Add(s);
            }

            return eligibleStudents;
        }


        [RelayCommand]
        private void DrawStudent()
        {
            if (AllSchoolClasses.Count == 0) return;
            var students = GetEligibleStudents(AllSchoolClasses[ClassIndex]);

            if (!StudentsVisible || students == null || students.Count == 0) { RandomStudent = ""; OnPropertyChanged(nameof(RandomStudent)); return; }
            ;

            Random rand = new Random();
            LinkedList<StudentVM> lastDrawnStudents = AllSchoolClasses[ClassIndex].LastDrawnStudents;

            StudentVM student = students[rand.Next(students.Count)];

            // TODO add lucky nr epic save animation
            //if (student.Id == _luckyNr) continue;

            RandomStudent = $"{student.Name} Nr. {student.IdNr}";
            lastDrawnStudents.AddLast(student);

            OnPropertyChanged(nameof(RandomStudent));
        }

        [RelayCommand]
        private async Task AddClassAsync()
        {
            // Display prompt and errror if needed
            Page page = App.Current.MainPage;
            string result = await page.DisplayPromptAsync("Nowa Klasa", "Nazwa Klasy:");

            if (result == null) return;
            if (string.IsNullOrWhiteSpace(result))
            {
                await page.DisplayAlert("Uwaga", "Wpisz nazwe poprawnie", "OK");
                return;
            }

            // Check if class already exists
            string className = PickerClassList.FirstOrDefault(s => s.ToLower().Trim() == result.Trim().ToLower(), "Nowa Klasa");

            if (className != "Nowa Klasa")
            {
                _classIndex = PickerClassList.IndexOf(className);
                OnPropertyChanged(nameof(_classIndex));
                return;
            }

            // Add new class to lists
            PickerClassList.Insert(ClassIndex, result);
            AllSchoolClasses.Add(new ClassM(result));
            AddClassVisible = false;
            setClassStudents(AllSchoolClasses.Last());
            StudentsVisible = true;

            OnPropertyChanged(nameof(AddClassVisible));
            OnPropertyChanged(nameof(StudentsVisible));
            OnPropertyChanged(nameof(ClassIndex));
            OnPropertyChanged(nameof(PickerClassList));
        }

        [RelayCommand]
        private async Task AddStudentAsync()
        {
            // Display prompt and errror if needed
            Page page = App.Current.MainPage;
            string result = await page.DisplayPromptAsync("Nowy Uczeń", "Nazwisko i Imię:");

            if (result == null) return;
            if (string.IsNullOrWhiteSpace(result))
            {
                await page.DisplayAlert("Uwaga", "Wpisz imię poprawnie", "OK");
                return;
            }
            // Add new person to lists
            StudentM newStudent = new StudentM();
            newStudent.Name = result;
            newStudent.Id = AllSchoolClasses[ClassIndex].Students.Count + 1;
            AllSchoolClasses[ClassIndex].Students.Add(new StudentVM(newStudent));
            setClassStudents(AllSchoolClasses[ClassIndex]);

        }

        [RelayCommand]
        private void RemoveStudent()
        {
            if (SelectedStudent == null) return;
            Debug.WriteLine(SelectedStudent.IdNr + "  Removed student " + SelectedStudent.Name);
            AllSchoolClasses[_classIndex].Students.Remove(SelectedStudent);
            AllSchoolClasses[_classIndex].SortStudents();
            setClassStudents(AllSchoolClasses[_classIndex]);
        }

        [RelayCommand]
        private async Task EditStudentAsync()
        {
            if (SelectedStudent == null) return;
            Debug.WriteLine("Editing student " + SelectedStudent.Name);

            Page page = App.Current.MainPage;
            string result = await page.DisplayPromptAsync("Edytujesz ucznia:", "Nazwisko i Imię:", initialValue: SelectedStudent.Name);

            if (result == null) return;
            if (string.IsNullOrWhiteSpace(result))
            {
                await page.DisplayAlert("Uwaga", "Wpisz imię poprawnie", "OK");
                return;
            }

            if (result == SelectedStudent.Name) return;

            SelectedStudent.Name = result;
            Debug.WriteLine(SelectedStudent.Name);

            AllSchoolClasses[_classIndex].SortStudents();

            setClassStudents(AllSchoolClasses[_classIndex]);
        }

        [RelayCommand]
        public async Task ExportAsync()
        {
            MemoryStream ms = new MemoryStream(Encoding.Default.GetBytes(SerializeToString()));
            var result = await FileSaver.Default.SaveAsync("Szkola.txt",ms);
            Debug.WriteLine(result.FilePath);
            if (!result.IsSuccessful)
            {
                Debug.WriteLine("Failed to Save file");
                Debug.WriteLine(result.Exception.ToString());
                App.Current.MainPage.DisplayAlert("Błąd", "Wystąpił błąd podczas eksportu pliku", "OK");

            }
        }

        /*
         * CLassName
         * {
         *  {
         *   IDNR
         *   StudentName
         *   present
         *  }
         *  [
         *   odlest drawn
         *   idnr2
         *   most recently drawn
         *  ]
         * }
         * ClassNmae2 
         * ...
         */
        private string SerializeToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (var schoolClass in AllSchoolClasses)
            {
                sb.AppendLine(schoolClass.Name);
                sb.AppendLine("{");
                foreach (var student in schoolClass.Students)
                {
                    sb.AppendLine("{");
                    sb.AppendLine($"{student.IdNr.ToString()};{student.Name};{student.Present.ToString()}");
                    sb.AppendLine("}");
                }
                sb.AppendLine("[");
                foreach (var student in schoolClass.LastDrawnStudents)
                {
                    sb.AppendLine(student.IdNr.ToString());
                }
                sb.AppendLine("]");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        [RelayCommand]
        public async Task ImportAsync()
        {
            try
            {
                var result = await FilePicker.Default.PickAsync();
                if (result != null)
                {
                    if (result.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    {
                        Load(result.FullPath);
                    }
                    else
                    {
                        await Shell.Current.CurrentPage.DisplayAlert("Błąd", "Wybierz plik .list.xml", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Błąd", $"Nie udało się zaimportować: {ex.Message}", "OK");
            }
        }

        private void Load(string filePath)
        {
            Debug.WriteLine("Loading");
            if (!File.Exists(filePath)) { Debug.WriteLine("Couldn't read file"); return; }
            bool readingStudent = false, readingLastDrawn = false;
            List<ClassM> classes = new List<ClassM>();
            ClassM? newClass = null;
            StudentM? newStudent = null;
            try
            {
                string[] contents = File.ReadAllLines(filePath);

                for (int i = 0; i < contents.Length; i++)
                {
                    string line = contents[i];
                    Debug.WriteLine(line);
                    if (newClass == null)
                    {
                        Debug.WriteLine("creating new class");
                        newClass = new ClassM(line);
                        i++;
                    }

                    else if (line == "{")
                    {
                        Debug.WriteLine("starting student read");
                        readingStudent = true;
                    }

                    else if (readingStudent)
                    {
                        if (line != "}")
                        {
                            Debug.WriteLine("reading student");
                            string[] splitLine = contents[i].Split(';');
                            newStudent = new StudentM() { Id = int.Parse(splitLine[0]), Name = splitLine[1], Present = bool.Parse(splitLine[2]) };
                        }
                        else
                        {
                            Debug.WriteLine("stopping reading student");
                            newClass.Students.Add(new StudentVM(newStudent));
                            newStudent = null;
                            readingStudent = false;
                        }
                    }
                    else if (line == "[")
                    {
                        Debug.WriteLine("starting reading last drawn");
                        readingLastDrawn = true;
                    }
                    else if (readingLastDrawn)
                    {
                        if (line != "]")
                        {
                            Debug.WriteLine("reading last drawn");
                            int idNr = int.Parse(line);
                            StudentVM student = newClass.Students.Find(s => s.IdNr == idNr);
                            newClass.LastDrawnStudents.AddLast(student);
                        }
                        else
                        {
                            Debug.WriteLine("stopping reading last drawn");
                            readingLastDrawn = false;
                        }
                    }
                    else if (line == "}" && !readingStudent)
                    {
                        Debug.WriteLine("stopping reading class");
                        classes.Add(newClass);
                        newClass = null;
                    }
                    else
                    {
                        Debug.WriteLine("nun to do");
                    }
                }
                AllSchoolClasses = classes;
                PickerClassList.Clear();
                _classIndex = 0;
                foreach (var sc in AllSchoolClasses)
                {
                    PickerClassList.Add(sc.Name);
                }
                PickerClassList.Add("Nowa Klasa");
                RefreshDisplay();
            }
            catch (Exception e)
            {
                Debug.WriteLine("sum went rong");
                Debug.WriteLine(e.ToString());
                App.Current.MainPage.DisplayAlert("Błąd", "Wystąpił błąd podczas wczytywania pliku", "OK");
            }
        }

        private void RefreshDisplay()
        {
            OnPropertyChanged(nameof(AddClassVisible));
            OnPropertyChanged(nameof(ClassIndex));
            OnPropertyChanged(nameof(StudentsVisible));
            OnPropertyChanged(nameof(RandomStudent));
            OnPropertyChanged(nameof(PickerClassList));
        }
    }
}
