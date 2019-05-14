using CsvHelper;
using Microsoft.Win32;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LetterClassifier
{
    class MainViewModel
    {
        private string _folderPath = @"\\Mac\Home\Documents\stat-602-2019-spring\plots";

        private List<ManualClassification> manualClassifications = new List<ManualClassification>();
        private List<int> xValues = new List<int>();

        public MainViewModel()
        {
            SaveClassificationsCommand.Subscribe(_ => SaveClassifications());
            LoadClassificationsCommand.Subscribe(_ => LoadClassifications());
            LabelImageCommand = new ReactiveCommand(Letter.Select(x => !string.IsNullOrEmpty(x)));
            LabelImageCommand.Subscribe(_ => LabelImage());
            LoadFolder();
        }

        private void LoadFolder() {
            xValues.Clear();
            var imageFiles = Directory.EnumerateFiles(_folderPath, "*.png");
            foreach(var imageFile in imageFiles)
            {
                var match = _imageFileNameRegex.Match(Path.GetFileName(imageFile));
                if (!match.Success)
                {
                    continue;
                }
                xValues.Add(int.Parse(match.Groups[1].Value));
            }
            PrepareToClassifyNextImage();
        }

        private void PrepareToClassifyNextImage()
        {
            foreach(var x in xValues)
            {
                if (!manualClassifications.Any(c => c.X == x))
                {
                    ImagePath.Value = _folderPath + "\\image" + x + ".png";
                    Letter.Value = "";
                    break;
                }
            }
        }

        private void LoadClassifications()
        {
            var fileOpenDlg = new OpenFileDialog();
            fileOpenDlg.DefaultExt = "*.csv";
            if (fileOpenDlg.ShowDialog() == true)
            {
                using (var streamReader = new StreamReader(fileOpenDlg.FileName))
                using (var reader = new CsvReader(streamReader))
                {
                    manualClassifications = reader.GetRecords<ManualClassification>().ToList();
                }
            }
            PrepareToClassifyNextImage();
        }

        private void SaveClassifications()
        {
            var fileSaveDlg = new SaveFileDialog();
            fileSaveDlg.DefaultExt = "*.csv";
            if (fileSaveDlg.ShowDialog() == true)
            {
                using (var streamWriter = new StreamWriter(fileSaveDlg.FileName))
                using (var writer = new CsvWriter(streamWriter))
                {
                    writer.WriteHeader<ManualClassification>();
                    writer.NextRecord();
                    writer.WriteRecords(manualClassifications);
                }
            }
        }

        private static Regex _imageFileNameRegex = new Regex(@"image(\d+)\.png");

        private void LabelImage()
        {
            var match = _imageFileNameRegex.Match(Path.GetFileName(ImagePath.Value));
            var capture = int.Parse(match.Groups[1].Value);

            manualClassifications.Add(new ManualClassification
            {
                Letter = Letter.Value,
                X = capture
            });

            PrepareToClassifyNextImage();
        }

        public ReactiveCommand LabelImageCommand { get; }
        public ReactiveCommand SaveClassificationsCommand { get; } = new ReactiveCommand();
        public ReactiveCommand LoadClassificationsCommand { get; } = new ReactiveCommand();

        public ReactiveProperty<string> ImagePath { get; } = new ReactiveProperty<string>();
        public ReactiveProperty<string> Letter { get; } = new ReactiveProperty<string>();
    }
}
