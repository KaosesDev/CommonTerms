using CommonTermsFunc;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CommonTerms.ViewModels.Pages
{
    /// <summary>
    /// View model for the dashboard page, providing a counter and command to increment it.
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        private const string StopWordsFileName = "ExcludeWords.txt";
        private const string UnigramsFileName = "commonTerms.txt";
        private const string BigramsFileName = "commonPhrases.txt";

        /// <summary>
        /// Gets or sets the counter value displayed on the dashboard.
        /// </summary>
        [ObservableProperty]
        private int _counter = 0;

        [ObservableProperty]
        private string _directoryPath;

        [ObservableProperty]
        private int _displayTopN = 10;

        // New: minimum word length to pass into BuildCommon
        [ObservableProperty]
        private int _minWordLength = 3;

        // Validation message for MinWordLength
        [ObservableProperty]
        private string _minWordLengthError;

        // New: recursive toggle
        [ObservableProperty]
        private bool _isRecursive = true;

        [ObservableProperty]
        private string _newStopWord;

        [ObservableProperty]
        private int _totalFileCount;

        public ObservableCollection<string> StopWords { get; }
        public ObservableCollection<KeyValuePair<string, int>> TopUnigrams { get; }
        public ObservableCollection<KeyValuePair<string, int>> TopBigrams { get; }

        // Keep full result sets in memory so we can display a limited subset
        // but still save the full lists to files.
        private List<KeyValuePair<string, int>> _allUnigrams = new();
        private List<KeyValuePair<string, int>> _allBigrams = new();

        public DashboardViewModel()     
        {
            StopWords = new ObservableCollection<string>();
            TopUnigrams = new ObservableCollection<KeyValuePair<string, int>>();
            TopBigrams = new ObservableCollection<KeyValuePair<string, int>>();
            MinWordLength = 3;
            MinWordLengthError = string.Empty;
            IsRecursive = false;
            DirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); // default
        }

        // Validate MinWordLength whenever it changes
        partial void OnMinWordLengthChanged(int value)
        {
            if (value < 1)
            {
                MinWordLengthError = "Min word length must be at least 1.";
            }
            else
            {
                MinWordLengthError = string.Empty;
            }
        }

        // When the DisplayTopN value changes refresh the UI collections from the
        // full lists kept in memory.
        partial void OnDisplayTopNChanged(int value)
        {
            RefreshDisplayedTops();
        }

        private void RefreshDisplayedTops()
        {
            // Ensure UI updates happen on the UI thread
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                TopUnigrams.Clear();
                foreach (var kv in _allUnigrams.Take(DisplayTopN))
                    TopUnigrams.Add(kv);

                TopBigrams.Clear();
                foreach (var kv in _allBigrams.Take(DisplayTopN))
                    TopBigrams.Add(kv);
            });
        }

        /// <summary>
        /// Increments the <see cref="Counter"/> property by one.
        /// </summary>
        [RelayCommand]
        private void OnCounterIncrement()
        {
            Counter++;
        }

        [RelayCommand]
        private void BrowseDirectory()
        {
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "Select directory to analyze",
                UseDescriptionForTitle = true,
                SelectedPath = DirectoryPath ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            // Owner the dialog to the main window (safer for modality)
            bool? ok = dlg.ShowDialog(Application.Current.MainWindow);
            if (ok == true)
            {
                DirectoryPath = dlg.SelectedPath;
            }
        }

        [RelayCommand]
        private void AddStopWord()
        {
            var w = NewStopWord?.Trim();
            if (!string.IsNullOrEmpty(w) && !StopWords.Contains(w, StringComparer.OrdinalIgnoreCase))
            {
                StopWords.Add(w);
            }
            NewStopWord = string.Empty;
        }

        [RelayCommand]
        private void RemoveStopWord(string word)
        {
            if (!string.IsNullOrEmpty(word))
            {
                var match = StopWords.FirstOrDefault(s => string.Equals(s, word, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    StopWords.Remove(match);
            }
        }

        [RelayCommand]
        private async Task SaveStopWordsAsync()
        {
            // Let the user pick the save location and filename
            var dlg = new VistaSaveFileDialog
            {
                Title = "Save Exclude Words",
                FileName = StopWordsFileName,
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                OverwritePrompt = true,
                InitialDirectory = !string.IsNullOrWhiteSpace(DirectoryPath) && Directory.Exists(DirectoryPath)
                    ? DirectoryPath
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
            if (result != true)
                return;

            var path = dlg.FileName;
            try
            {
                await File.WriteAllLinesAsync(path, StopWords);
                MessageBox.Show($"Exclude words saved to: {path}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save Exclude words: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task LoadStopWordsAsync()
        {
            // Let the user pick the file to load
            var dlg = new VistaOpenFileDialog
            {
                Title = "Load Exclude Words",
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Multiselect = false,
                InitialDirectory = !string.IsNullOrWhiteSpace(DirectoryPath) && Directory.Exists(DirectoryPath)
                    ? DirectoryPath
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
            if (result != true)
                return;

            var path = dlg.FileName;
            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show($"Exclude words file not found: {path}", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var lines = await File.ReadAllLinesAsync(path);
                StopWords.Clear();
                foreach (var l in lines.Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                    StopWords.Add(l);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load Exclude words: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RunAnalysisAsync()
        {
            // Validate MinWordLength before starting
            if (MinWordLength < 1)
            {
                MinWordLengthError = "Min word length must be at least 1.";
                return;
            }
            else
            {
                MinWordLengthError = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(DirectoryPath) || !Directory.Exists(DirectoryPath))
            {
                MessageBox.Show("Please enter or browse to a valid directory first.", "Invalid directory", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await Task.Run(() =>
            {
                try
                {
                    var builder = new FileListBuilder(DirectoryPath);
                    List<string> filePaths;

                    if (IsRecursive)
                        filePaths = builder.GetAllFilesRecursively();
                    else
                        filePaths = builder.GetAllFiles();

                    var fileModels = builder.BuildModelList(filePaths);

                    // Pass the MinWordLength into BuildCommon constructor
                    var bc = new BuildCommon(fileModels, MinWordLength);

                    // Add stop words to BuildCommon
                    foreach (var sw in StopWords)
                        bc.AddWordsToExclude(sw);

                    bc.Build();

                    // Keep the full ordered lists in memory, and only display the
                    // top N in the UI collections.
                    var unigramsFull = bc.GetUnigramCounts()
                                          .OrderByDescending(kv => kv.Value)
                                          .ToList();

                    var bigramsFull = bc.GetBigramCounts()
                                         .OrderByDescending(kv => kv.Value)
                                         .ToList();

                    // Store full lists for saving later
                    _allUnigrams = unigramsFull;
                    _allBigrams = bigramsFull;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // Populate UI collections with only the top N entries
                        TopUnigrams.Clear();
                        foreach (var kv in _allUnigrams.Take(DisplayTopN))
                            TopUnigrams.Add(kv);

                        TopBigrams.Clear();
                        foreach (var kv in _allBigrams.Take(DisplayTopN))
                            TopBigrams.Add(kv);

                        TotalFileCount = fileModels.Count;
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"Analysis failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });
        }

        [RelayCommand]
        private async Task SaveUnigramsAsync()
        {
            // Let the user pick the save location and filename for unigrams
            var dlg = new VistaSaveFileDialog
            {
                Title = "Save Common Terms",
                FileName = UnigramsFileName,
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                OverwritePrompt = true,
                InitialDirectory = !string.IsNullOrWhiteSpace(DirectoryPath) && Directory.Exists(DirectoryPath)
                    ? DirectoryPath
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
            if (result != true)
                return;

            var path = dlg.FileName;
            try
            {
                var unigramSource = (_allUnigrams != null && _allUnigrams.Any()) ? (IEnumerable<KeyValuePair<string, int>>)_allUnigrams : TopUnigrams;
                var lines = unigramSource.Select(kv => $"{kv.Key}\t{kv.Value}");
                await File.WriteAllLinesAsync(path, lines);
                MessageBox.Show($"Common Terms saved to: {path}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save Common Terms: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task SaveBigramsAsync()
        {
            // Let the user pick the save location and filename for bigrams
            var dlg = new VistaSaveFileDialog
            {
                Title = "Save Common Phrases",
                FileName = BigramsFileName,
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                OverwritePrompt = true,
                InitialDirectory = !string.IsNullOrWhiteSpace(DirectoryPath) && Directory.Exists(DirectoryPath)
                    ? DirectoryPath
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
            };

            bool? result = dlg.ShowDialog(Application.Current.MainWindow);
            if (result != true)
                return;

            var path = dlg.FileName;
            try
            {
                var bigramSource = (_allBigrams != null && _allBigrams.Any()) ? (IEnumerable<KeyValuePair<string, int>>)_allBigrams : TopBigrams;
                var lines = bigramSource.Select(kv => $"{kv.Key}\t{kv.Value}");
                await File.WriteAllLinesAsync(path, lines);
                MessageBox.Show($"Common Phrases saved to: {path}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save Common Phrases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper: downloads folder path (UserProfile + "Downloads")
        private static string GetDownloadsFilePath(string fileName)
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var downloads = Path.Combine(userProfile, "Downloads");
            if (!Directory.Exists(downloads))
                Directory.CreateDirectory(downloads);
            return Path.Combine(downloads, fileName);
        }
    }
}