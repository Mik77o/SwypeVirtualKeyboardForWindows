using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Linq;
using WindowsInput;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using WindowsInput.Native;

namespace Klawiatura
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public InputSimulator inpSimulator = new InputSimulator();

        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        enum State { active, notactive };

        int textBoxCounter = 0;
        int switchDrawing = 0;
        int changeSizeCounter = 0;

        string path = "";

        bool capturing = false;
        bool drawPolyline = false;
        bool fastAddingSuggestion = true;
        bool switchPersonalDictionary = true;

        State state = State.active;

        Image image = new Image();
        Image imageChangeSize = new Image();
        Image imageBlocking = new Image();

        public MainWindow()
        {
            SourceInitialized += (s, e) =>
            {
                var windowInteropHelper = new WindowInteropHelper(this);
                int exStyle = GetWindowLong(windowInteropHelper.Handle, GWL_EXSTYLE);
                SetWindowLong(windowInteropHelper.Handle, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
            };

            InitializeComponent();
            pol.IsChecked = true;
        }

        public static IEnumerable<T> FindVisualComponents<T>(DependencyObject obj) where T : DependencyObject
        {
            if (obj != null)
            {
                for (int n = 0; n < VisualTreeHelper.GetChildrenCount(obj); n++)
                {
                    DependencyObject childDO = VisualTreeHelper.GetChild(obj, n);
                    if (childDO != null && childDO is T)
                    {
                        yield return (T)childDO;
                    }

                    foreach (T childOfChildDO in FindVisualComponents<T>(childDO))
                    {
                        yield return childOfChildDO;
                    }
                }
            }
        }


        public void ClickedNotSwypeButtons(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cmd = button.Tag as string;
            var selLength = textBoxOfSwypeKeyboard.SelectionLength;
            var selStart = textBoxOfSwypeKeyboard.SelectionStart;

            if (cmd == "Backspace")
            {
                if (state == State.active)
                {
                    if (selLength > 0)
                    {
                        InsertText("");
                    }
                    else if (selStart > 0)
                    {
                        textBoxOfSwypeKeyboard.Text = textBoxOfSwypeKeyboard.Text.Substring(0, selStart - 1) + textBoxOfSwypeKeyboard.Text.Substring(selStart);
                        textBoxOfSwypeKeyboard.SelectionStart = selStart - 1;
                    }
                }

                if (state == State.notactive)
                {
                    inpSimulator.Keyboard.KeyPress(VirtualKeyCode.BACK);
                }
            }
            else if (cmd == "blocking")
            {
                textBoxCounter++;
                if (textBoxCounter % 2 == 1)
                {
                    state = State.notactive;
                    imageBlocking.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/padlock.png"));
                    buttBlocked.Content = imageBlocking;
                    textBoxOfSwypeKeyboard.Text = "Textbox blocked!";
                }
                else if (textBoxCounter % 2 == 0)
                {
                    state = State.active;
                    textBoxOfSwypeKeyboard.Text = "";
                    imageBlocking.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/unlocked_padlock.png"));
                    buttBlocked.Content = imageBlocking;
                }
            }
            else if (cmd == "Enter")
            {
                if (state == State.active)
                    InsertText("\n");
                if (state == State.notactive)
                    inpSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            }
            else if (cmd == "...")
            {
                if (state == State.notactive)
                    inpSimulator.Keyboard.TextEntry("...");
                if (state == State.active)
                    InsertText("...");
            }
            else if (cmd == "Tab")
            {
                if (state == State.active)
                    InsertText("\t");
                if (state == State.notactive)
                    inpSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            }
            else if (cmd == "Space")
            {
                if (state == State.active)
                    InsertText(" ");
                if (state == State.notactive)
                    inpSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
            }
            else if (cmd == "Clear")
            {
                textBoxOfSwypeKeyboard.Text = "";
                //changeSizeCounter = 0;
                inpSimulator.Keyboard.KeyPress(VirtualKeyCode.DELETE);
            }
            else if (cmd == "clr")
            {
                if (switchPersonalDictionary)
                {
                    using (var fs = new FileStream("PersonalDictionary.txt", FileMode.Truncate))
                    {
                    }
                    MessageBox.Show("Personal dictionary is removed!", "Deleting personal dictionary", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Console.WriteLine("Personal dictionary is removed!");
                }
                else
                    MessageBox.Show("Personal dictionary is not active. You cannot remove it.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (cmd == "addWord")
            {
                if (switchPersonalDictionary)
                {
                    string wordToDict;
                    wordToDict = textBoxOfSwypeKeyboard.Text;
                    if (wordToDict != null && wordToDict.Count() > 1 && !wordToDict.Contains(" ") && !wordToDict.Contains("\t") && !wordToDict.Contains("\n"))
                    {
                        File.AppendAllText("PersonalDictionary.txt", wordToDict.ToLower() + Environment.NewLine);
                        MessageBox.Show("The word has been added!", "Adding word", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("TextBox contains whitespace, single character or empty string! It's not possible to add word!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                    MessageBox.Show("Personal dictionary is not active. You cannot add word!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (cmd == "draw")
            {

                if (switchDrawing % 2 == 1)
                {
                    drawPolyline = false;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/notDrawPoly.png"));
                    buttonDraw.Content = image;

                }
                else
                if (switchDrawing % 2 == 0)
                {
                    drawPolyline = true;
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/drawPoly.png"));
                    buttonDraw.Content = image;
                }
                switchDrawing++;
            }

            else if (cmd == "change")
            {
                changeSizeCounter++;
                if (changeSizeCounter % 3 == 0)
                {
                    imageChangeSize.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/small.png"));
                    LinearGradientBrush gradientBrushOn = new LinearGradientBrush(Color.FromRgb(238, 238, 238), Color.FromRgb(0, 0, 0), new Point(0.5, 0), new Point(0.5, 1));
                    GradientStop GradientStop = new GradientStop();
                    GradientStop.Offset = 0.847;
                    gradientBrushOn.GradientStops.Add(GradientStop);
                    buttonConvertWords.Background = gradientBrushOn;
                    buttona.Content = "a";
                    buttonb.Content = "b";
                    buttonc.Content = "c";
                    buttond.Content = "d";
                    buttone.Content = "e";
                    buttonf.Content = "f";
                    buttong.Content = "g";
                    buttonh.Content = "h";
                    buttoni.Content = "i";
                    buttonj.Content = "j";
                    buttonk.Content = "k";
                    buttonl.Content = "l";
                    buttonm.Content = "m";
                    buttonn.Content = "n";
                    buttono.Content = "o";
                    buttonp.Content = "p";
                    buttonr.Content = "r";
                    buttons.Content = "s";
                    buttont.Content = "t";
                    buttonu.Content = "u";
                    buttony.Content = "y";
                    buttonw.Content = "w";
                    buttonz.Content = "z";
                    buttonq.Content = "q";
                    buttonv.Content = "v";
                    buttonx.Content = "x";
                    buttonaMain.Content = "ą";
                    buttoneMain.Content = "ę";
                    buttoncMain.Content = "ć";
                    buttonsMain.Content = "ś";
                    buttonnMain.Content = "ń";
                    buttonźMain.Content = "ź";
                    buttonżMain.Content = "ż";
                    buttonlMain.Content = "ł";
                    buttonoMain.Content = "ó";
                }

                if (changeSizeCounter % 3 == 1)
                {
                    imageChangeSize.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/upper.png"));
                    buttonConvertWords.Content = imageChangeSize;

                    buttona.Content = "A";
                    buttonb.Content = "B";
                    buttonc.Content = "C";
                    buttond.Content = "D";
                    buttone.Content = "E";
                    buttonf.Content = "F";
                    buttong.Content = "G";
                    buttonh.Content = "H";
                    buttoni.Content = "I";
                    buttonj.Content = "J";
                    buttonk.Content = "K";
                    buttonl.Content = "L";
                    buttonm.Content = "M";
                    buttonn.Content = "N";
                    buttono.Content = "O";
                    buttonp.Content = "P";
                    buttonr.Content = "R";
                    buttons.Content = "S";
                    buttont.Content = "T";
                    buttonu.Content = "U";
                    buttony.Content = "Y";
                    buttonw.Content = "W";
                    buttonz.Content = "Z";
                    buttonq.Content = "Q";
                    buttonv.Content = "V";
                    buttonx.Content = "X";

                    buttonaMain.Content = "Ą";
                    buttoneMain.Content = "Ę";
                    buttoncMain.Content = "Ć";
                    buttonsMain.Content = "Ś";
                    buttonnMain.Content = "Ń";
                    buttonźMain.Content = "Ź";
                    buttonżMain.Content = "Ż";
                    buttonlMain.Content = "Ł";
                    buttonoMain.Content = "Ó";


                }
                else if (changeSizeCounter % 3 == 2)
                {
                    imageChangeSize.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/capsLockOn.png"));
                    buttonConvertWords.Content = imageChangeSize;

                }
            }

            else if (cmd == "ToLower")
            {
                if (textBoxCounter % 2 != 1)
                    TransformString(x => x.ToLower());
            }
            else if (cmd == "ToUpper")
            {
                if (textBoxCounter % 2 != 1)
                    TransformString(x => x.ToUpper());
            }
            else if (cmd == "Save")
            {
            }

            textBoxOfSwypeKeyboard.Focus();
            e.Handled = true;
        }

        public void Mousemove(object sender, MouseEventArgs e)
        {
            if (capturing && drawPolyline)
            {
                polylineTraceOfPath.Points.Add(e.GetPosition(canvas));
            }
            //ShowPath("mousemove");
        }

        public void Mouseenter(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.Foreground = Brushes.Black;
            buttonSave.Foreground = Brushes.Black;
            buttonTab.Foreground = Brushes.Black;
            buttonToLower.Foreground = Brushes.Black;
            buttonToUpper.Foreground = Brushes.Black;
            buttonClear.Foreground = Brushes.Black;
            space.Foreground = Brushes.Black;
            var cmd = button.Tag as string;
            if (cmd.Length > 1)
                return;

            if (capturing)
            {
                if (path.Length > 0 && path[path.Length - 1] == cmd[0])

                    return;

                path += cmd;
                ShowPath("mouseenter");
            }
        }

        public void Mouseleave(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
                button.IsEnabled = true;
            button.Foreground = Brushes.White;
            buttonSave.Foreground = Brushes.Black;
            buttonTab.Foreground = Brushes.Black;
            buttonToLower.Foreground = Brushes.Black;
            buttonToUpper.Foreground = Brushes.Black;
            buttonClear.Foreground = Brushes.Black;
            space.Foreground = Brushes.Black;
            //ShowPath("mouseleave");
        }

        public void StopCapturing(object sender, RoutedEventArgs e)
        {
            if (capturing)
            {

                polylineTraceOfPath.Points.Clear();
                Console.WriteLine("END OF CAPTURE");
                capturing = false;

                if (path.Length == 1)
                {
                    if (state == State.active)
                    {
                        if (changeSizeCounter % 3 == 1)
                        {
                            InsertText(path.ToUpper());
                            changeSizeCounter = 0;
                            buttona.Content = "a";
                            buttonb.Content = "b";
                            buttonc.Content = "c";
                            buttond.Content = "d";
                            buttone.Content = "e";
                            buttonf.Content = "f";
                            buttong.Content = "g";
                            buttonh.Content = "h";
                            buttoni.Content = "i";
                            buttonj.Content = "j";
                            buttonk.Content = "k";
                            buttonl.Content = "l";
                            buttonm.Content = "m";
                            buttonn.Content = "n";
                            buttono.Content = "o";
                            buttonp.Content = "p";
                            buttonr.Content = "r";
                            buttons.Content = "s";
                            buttont.Content = "t";
                            buttonu.Content = "u";
                            buttony.Content = "y";
                            buttonw.Content = "w";
                            buttonz.Content = "z";
                            buttonq.Content = "q";
                            buttonv.Content = "v";
                            buttonx.Content = "x";
                            buttonaMain.Content = "ą";
                            buttoneMain.Content = "ę";
                            buttoncMain.Content = "ć";
                            buttonsMain.Content = "ś";
                            buttonnMain.Content = "ń";
                            buttonźMain.Content = "ź";
                            buttonżMain.Content = "ż";
                            buttonlMain.Content = "ł";
                            buttonoMain.Content = "ó";
                            LinearGradientBrush gradientBrushOn = new LinearGradientBrush(Color.FromRgb(238, 238, 238), Color.FromRgb(0, 0, 0), new Point(0.5, 0), new Point(0.5, 1));
                            GradientStop GradientStop = new GradientStop();
                            GradientStop.Offset = 0.847;
                            gradientBrushOn.GradientStops.Add(GradientStop);
                            buttonConvertWords.Background = gradientBrushOn;
                            imageChangeSize.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/small.png"));
                            changeSizeCounter = 0;
                        }
                        else if (changeSizeCounter % 3 == 2)
                        {
                            InsertText(path.ToUpper());
                        }
                        else if (changeSizeCounter % 3 == 0)
                        {
                            InsertText(path);
                        }
                    }
                    else

                    {
                        if (changeSizeCounter % 3 == 1)
                        {
                            inpSimulator.Keyboard.TextEntry(path.ToUpper());
                            changeSizeCounter = 0;
                            buttona.Content = "a";
                            buttonb.Content = "b";
                            buttonc.Content = "c";
                            buttond.Content = "d";
                            buttone.Content = "e";
                            buttonf.Content = "f";
                            buttong.Content = "g";
                            buttonh.Content = "h";
                            buttoni.Content = "i";
                            buttonj.Content = "j";
                            buttonk.Content = "k";
                            buttonl.Content = "l";
                            buttonm.Content = "m";
                            buttonn.Content = "n";
                            buttono.Content = "o";
                            buttonp.Content = "p";
                            buttonr.Content = "r";
                            buttons.Content = "s";
                            buttont.Content = "t";
                            buttonu.Content = "u";
                            buttony.Content = "y";
                            buttonw.Content = "w";
                            buttonz.Content = "z";
                            buttonq.Content = "q";
                            buttonv.Content = "v";
                            buttonx.Content = "x";
                            buttonaMain.Content = "ą";
                            buttoneMain.Content = "ę";
                            buttoncMain.Content = "ć";
                            buttonsMain.Content = "ś";
                            buttonnMain.Content = "ń";
                            buttonźMain.Content = "ź";
                            buttonżMain.Content = "ż";
                            buttonlMain.Content = "ł";
                            buttonoMain.Content = "ó";

                            LinearGradientBrush gradientBrushOn = new LinearGradientBrush(Color.FromRgb(238, 238, 238), Color.FromRgb(0, 0, 0), new Point(0.5, 0), new Point(0.5, 1));
                            GradientStop GradientStop = new GradientStop();
                            GradientStop.Offset = 0.847;
                            gradientBrushOn.GradientStops.Add(GradientStop);
                            buttonConvertWords.Background = gradientBrushOn;
                            imageChangeSize.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/small.png"));
                            changeSizeCounter = 0;
                        }
                        else if (changeSizeCounter % 3 == 2)
                        {
                            inpSimulator.Keyboard.TextEntry(path.ToUpper());
                        }
                        else if (changeSizeCounter % 3 == 0)
                        {
                            inpSimulator.Keyboard.TextEntry(path);
                        }

                    }
                }
                else
                {
                    if (path.Length >= 2 && changeSizeCounter % 3 == 1)
                    {
                        buttona.Content = "a";
                        buttonb.Content = "b";
                        buttonc.Content = "c";
                        buttond.Content = "d";
                        buttone.Content = "e";
                        buttonf.Content = "f";
                        buttong.Content = "g";
                        buttonh.Content = "h";
                        buttoni.Content = "i";
                        buttonj.Content = "j";
                        buttonk.Content = "k";
                        buttonl.Content = "l";
                        buttonm.Content = "m";
                        buttonn.Content = "n";
                        buttono.Content = "o";
                        buttonp.Content = "p";
                        buttonr.Content = "r";
                        buttons.Content = "s";
                        buttont.Content = "t";
                        buttonu.Content = "u";
                        buttony.Content = "y";
                        buttonw.Content = "w";
                        buttonz.Content = "z";
                        buttonq.Content = "q";
                        buttonv.Content = "v";
                        buttonx.Content = "x";
                        buttonaMain.Content = "ą";
                        buttoneMain.Content = "ę";
                        buttoncMain.Content = "ć";
                        buttonsMain.Content = "ś";
                        buttonnMain.Content = "ń";
                        buttonźMain.Content = "ź";
                        buttonżMain.Content = "ż";
                        buttonlMain.Content = "ł";
                        buttonoMain.Content = "ó";
                    }

                    if (switchPersonalDictionary)
                    {
                        Console.WriteLine("Loading personal dictionary...");
                        string[] lines = File.ReadAllLines("PersonalDictionary.txt");
                        File.WriteAllLines("PersonalDictionary.txt", lines.Distinct().ToArray());
                        Swipe.LoadWordsFromPersonalDictionary();
                        Console.WriteLine("Personal dictionary was loaded");
                        var suggestionsPersonal = Swipe.GetMostAppropriateSuggestionsFromPersonalDictionary(path);
                        if (suggestionsPersonal.Count != 0)
                        {


                            Console.WriteLine(string.Join(", ", suggestionsPersonal));
                            ClearSuggestions();
                            var spaceBetweenSuggestions = 15.0;
                            var x = 0.0;
                            var x_1 = 0.0;
                            var x_2 = 0.0;
                            var x_3 = 0.0;

                            foreach (var word in suggestionsPersonal)
                            {
                                var btn = new Button
                                {
                                    Content = (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper()),
                                    Tag = (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())
                                };

                                btn.MouseLeave += PreviousSizeOfButton;
                                btn.MouseMove += IncreaseSizeOfButton;
                                btn.Click += SuggestionClicked;
                                btn.FontSize = 15;
                                btn.Background = Brushes.White;
                                btn.Foreground = Brushes.Black;
                                btn.FontWeight = FontWeights.Bold;
                                canvasSuggestions.Children.Add(btn);
                                if (x < 1000)
                                {
                                    Canvas.SetLeft(btn, x);
                                    x += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }
                                if (x > 1000)
                                {
                                    Canvas.SetLeft(btn, x_1);
                                    Canvas.SetTop(btn, 50);
                                    canvasSuggestions.Height = 125;
                                    x_1 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }
                                if (x_1 > 1000)
                                {
                                    Canvas.SetLeft(btn, x_2);
                                    Canvas.SetTop(btn, 100);
                                    canvasSuggestions.Height = 140;
                                    x_2 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }

                                if (x_2 > 1000)
                                {
                                    Canvas.SetLeft(btn, x_3);
                                    Canvas.SetTop(btn, 150);
                                    canvasSuggestions.Height = 180;
                                    x_3 += PrepareStringOfButton(btn, (changeSizeCounter % 3 != 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Personal dictionary doesn't cotain this word");
                            var suggestions = Swipe.GetMostAppropriateSuggestions(path);

                            Console.WriteLine(string.Join(", ", suggestions));
                            ClearSuggestions();
                            var spaceBetweenSuggestions = 15.0;
                            var x = 0.0;
                            var x_1 = 0.0;
                            var x_2 = 0.0;
                            var x_3 = 0.0;

                            foreach (var word in suggestions)
                            {
                                var btn = new Button
                                {
                                    Content = (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper()),
                                    Tag = (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())
                                };

                                btn.MouseLeave += PreviousSizeOfButton;
                                btn.MouseMove += IncreaseSizeOfButton;
                                btn.Click += SuggestionClicked;
                                btn.FontSize = 15;
                                btn.Background = Brushes.White;
                                btn.Foreground = Brushes.Black;
                                btn.FontWeight = FontWeights.Bold;
                                canvasSuggestions.Children.Add(btn);
                                if (x < 1000)
                                {
                                    Canvas.SetLeft(btn, x);
                                    x += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }
                                if (x > 1000)
                                {
                                    Canvas.SetLeft(btn, x_1);
                                    Canvas.SetTop(btn, 50);
                                    canvasSuggestions.Height = 125;
                                    x_1 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }
                                if (x_1 > 1000)
                                {
                                    Canvas.SetLeft(btn, x_2);
                                    Canvas.SetTop(btn, 100);
                                    canvasSuggestions.Height = 140;
                                    x_2 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }

                                if (x_2 > 1000)
                                {
                                    Canvas.SetLeft(btn, x_3);
                                    Canvas.SetTop(btn, 150);
                                    canvasSuggestions.Height = 180;
                                    x_3 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                                }
                            }

                        }
                    }
                    else
                    {
                        var suggestions = Swipe.GetMostAppropriateSuggestions(path);

                        Console.WriteLine("Personal dictionary is not active!");
                        Console.WriteLine(string.Join(", ", suggestions));
                        ClearSuggestions();
                        var spaceBetweenSuggestions = 15.0;
                        var x = 0.0;
                        var x_1 = 0.0;
                        var x_2 = 0.0;
                        var x_3 = 0.0;

                        foreach (var word in suggestions)
                        {
                            var btn = new Button
                            {
                                Content = (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper()),
                                Tag = (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())
                            };

                            btn.MouseLeave += PreviousSizeOfButton;
                            btn.MouseMove += IncreaseSizeOfButton;
                            btn.Click += SuggestionClicked;
                            btn.FontSize = 15;
                            btn.Background = Brushes.White;
                            btn.Foreground = Brushes.Black;
                            btn.FontWeight = FontWeights.Bold;
                            canvasSuggestions.Children.Add(btn);
                            if (x < 1000)
                            {
                                Canvas.SetLeft(btn, x);
                                x += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                            }
                            if (x > 1000)
                            {
                                Canvas.SetLeft(btn, x_1);
                                Canvas.SetTop(btn, 50);
                                canvasSuggestions.Height = 125;
                                x_1 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                            }
                            if (x_1 > 1000)
                            {
                                Canvas.SetLeft(btn, x_2);
                                Canvas.SetTop(btn, 100);
                                canvasSuggestions.Height = 140;
                                x_2 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                            }

                            if (x_2 > 1000)
                            {
                                Canvas.SetLeft(btn, x_3);
                                Canvas.SetTop(btn, 150);
                                canvasSuggestions.Height = 180;
                                x_3 += PrepareStringOfButton(btn, (changeSizeCounter % 3 == 0 ? word : changeSizeCounter % 3 == 1 ? FirstCharToUpper(word) : word.ToUpper())).Width + spaceBetweenSuggestions;
                            }
                        }

                    }
                }
            }
        }

        private Size PrepareStringOfButton(Button button, string modifiedSuggestion)
        {
            var preparedText = new FormattedText(
                modifiedSuggestion,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(button.FontFamily, button.FontStyle, button.FontWeight, button.FontStretch),
                button.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                TextFormattingMode.Display
            );
            return new Size(preparedText.Width, preparedText.Height);
        }

        private void ClearSuggestions()
        {
            canvasSuggestions.Children.RemoveRange(0, canvasSuggestions.Children.Count);
        }

        public void SuggestionClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var word = button.Tag as string;

            if (state == State.notactive)
            {
                inpSimulator.Keyboard.TextEntry(word);
            }
            else
            {
                InsertText(word + " ");
            }
            if (switchPersonalDictionary)
            {
                if (changeSizeCounter % 3 == 1)
                {

                    File.AppendAllText("PersonalDictionary.txt", FirstCharToLower(word) + Environment.NewLine);
                }
                else if (changeSizeCounter % 3 == 2)
                {
                    File.AppendAllText("PersonalDictionary.txt", word.ToLower() + Environment.NewLine);
                }
                else if (changeSizeCounter % 3 == 0)
                {
                    File.AppendAllText("PersonalDictionary.txt", word + Environment.NewLine);
                }
            }

            if (changeSizeCounter % 3 == 1)
            {
                LinearGradientBrush gradientBrushOn = new LinearGradientBrush(Color.FromRgb(238, 238, 238), Color.FromRgb(0, 0, 0), new Point(0.5, 0), new Point(0.5, 1));
                GradientStop GradientStop = new GradientStop();
                GradientStop.Offset = 0.847;
                gradientBrushOn.GradientStops.Add(GradientStop);
                buttonConvertWords.Background = gradientBrushOn;
                imageChangeSize.Source = new BitmapImage(new Uri("pack://application:,,,/imageButton/small.png"));
                changeSizeCounter = 0;
            }
            canvasSuggestions.Height = 125;
            ClearSuggestions();
        }

        public void IncreaseSizeOfButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var word = button.Tag as string;
            button.FontSize = 20;
            button.FontWeight = FontWeights.ExtraLight;
            if (fastAddingSuggestion)
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                canvasSuggestions.Height = 125;
            }
        }

        public void PreviousSizeOfButton(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.FontSize = 15;
            button.FontWeight = FontWeights.Bold;
        }

        public static string FirstCharToUpper(string word)
        {
            if (String.IsNullOrEmpty(word))
                throw new ArgumentException("Error - empty String!");
            return word.First().ToString().ToUpper() + String.Join("", word.Skip(1));
        }

        public static string FirstCharToLower(string word)
        {
            if (String.IsNullOrEmpty(word))
                throw new ArgumentException("Error - empty String!");
            return word.First().ToString().ToLower() + String.Join("", word.Skip(1));
        }

        public void Mousedown(object sender, RoutedEventArgs e)
        {
            canvasSuggestions.Height = 125;
            ClearSuggestions();
            var button = sender as Button;
            var cmd = button.Tag as string;
            if (cmd.Length > 1)
                return;

            if (!capturing)
            {
                capturing = true;
                path = "";
                ShowPath("mousedown");
            }
            else
            {
                capturing = false;
            }

            textBoxOfSwypeKeyboard.Focus();
            button.IsEnabled = false;
        }

        private void ShowPath(string s)
        {
            Console.WriteLine(s + " -- " + path);
        }

        private void InsertText(string tag)
        {
            int cursorPlace = textBoxOfSwypeKeyboard.SelectionStart;
            if (textBoxOfSwypeKeyboard.SelectionLength > 0)
            {
                textBoxOfSwypeKeyboard.Text = textBoxOfSwypeKeyboard.Text.Substring(0, cursorPlace) + textBoxOfSwypeKeyboard.Text.Substring(cursorPlace + textBoxOfSwypeKeyboard.SelectionLength);
            }
            textBoxOfSwypeKeyboard.Text = textBoxOfSwypeKeyboard.Text.Insert(cursorPlace, tag);
            textBoxOfSwypeKeyboard.SelectionStart = cursorPlace + tag.Length;
        }

        private void TransformString(Func<string, string> transformation)
        {
            if (textBoxOfSwypeKeyboard.SelectionLength > 0)
                textBoxOfSwypeKeyboard.SelectedText = transformation(textBoxOfSwypeKeyboard.SelectedText);
            else
                textBoxOfSwypeKeyboard.Text = transformation(textBoxOfSwypeKeyboard.Text);
        }

        //funkcja reagująca na zdarzenia dla textBox
        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
        }

        //funkcja czyszcząca zawartość okna
        private void ClearContentOfTextBoxAndSuggestions(object sender, RoutedEventArgs e)
        {
            textBoxOfSwypeKeyboard.Clear();
            ClearSuggestions();
            canvasSuggestions.Height = 125;
        }

        //funkcja zapisująca zawartość okna do pliku tekstowego o określonej nazwie
        private void SaveContentOfTextBoxToFile(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Document (.txt)|*.txt|Microsoft Document 97-2003 (.doc)|*.doc";
            saveFileDialog.Title = "Zapisz do pliku";
            saveFileDialog.ShowDialog();
            string nameOfFile = saveFileDialog.FileName;
            if (nameOfFile.Length > 0)
                File.WriteAllText(nameOfFile, textBoxOfSwypeKeyboard.Text);
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var button in FindVisualComponents<Button>(this))
            {
                button.MouseLeave += Mouseleave;
                button.Click += ClickedNotSwypeButtons;
                button.MouseEnter += Mouseenter;
                button.PreviewMouseDown += Mousedown;
                button.MouseMove += Mousemove;
            }

            PreviewMouseUp += StopCapturing;
            MouseLeave += StopCapturing;
            foreach (var control in FindVisualComponents<Control>(this))
            {
                control.PreviewMouseUp += StopCapturing;
            }
        }

        private void WindowAtivated(object sender, EventArgs e)
        {
            textBoxOfSwypeKeyboard.Focus();
        }

        private void WindowDeactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void WindowGotFocus(object sender, RoutedEventArgs e)
        {
            textBoxOfSwypeKeyboard.Focus();
        }

        private void IncreaseButton(object sender, MouseEventArgs e)
        {
            buttBlocked.Width = 52;
            buttBlocked.Height = 52;
        }

        private void DecreaseButton(object sender, MouseEventArgs e)
        {
            buttBlocked.Width = 16;
            buttBlocked.Height = 16;
        }

        private void PolChecked(object sender, RoutedEventArgs e)
        {
            eng.IsChecked = false;
            pol.Foreground = Brushes.Red;
            space.Content = "polish";
            space.FontSize = 14;
            Swipe.SetKindOfDictionary("wordlist.txt");
            Console.WriteLine("Loading polish dictionary...");
            Swipe.LoadWordsFromDictionary();
            Console.WriteLine("Polish dictionary was loaded.");
        }
        private void EngChecked(object sender, RoutedEventArgs e)
        {
            pol.IsChecked = false;
            eng.Foreground = Brushes.Red;
            space.Content = "english";
            space.FontSize = 14;
            Swipe.SetKindOfDictionary("wordlist_ang.txt");
            Console.WriteLine("Loading english dictionary...");
            Swipe.LoadWordsFromDictionary();
            Console.WriteLine("English dictionary was loaded.");
        }
        private void PolUnchecked(object sender, RoutedEventArgs e)
        {
            eng.IsChecked = true;
            pol.Foreground = Brushes.Black;
        }
        private void EngUnchecked(object sender, RoutedEventArgs e)
        {
            pol.IsChecked = true;
            eng.Foreground = Brushes.Black;
        }

        private void PersonalDictionaryActivated(object sender, RoutedEventArgs e)
        {
            switchPersonalDictionary = true;
            choosePersDict.Foreground = Brushes.Red;
            Console.WriteLine("Personal Dictionary is active!");
        }

        private void PersonalDictionaryNotActivated(object sender, RoutedEventArgs e)
        {
            switchPersonalDictionary = false;
            choosePersDict.Foreground = Brushes.Black;
            Console.WriteLine("AutoSuggestionsOn!");
        }

        private void AutoSuggestionsActivated(object sender, RoutedEventArgs e)
        {
            fastAddingSuggestion = true;
            AutoSuggMode.Foreground = Brushes.Red;
        }

        private void AutoSuggestionsNotActivated(object sender, RoutedEventArgs e)
        {
            fastAddingSuggestion = false;
            AutoSuggMode.Foreground = Brushes.Black;
            Console.WriteLine("AutoSuggestions Off!");
        }
    }
}



