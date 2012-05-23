/******************************************************/
/*** Progetto di De Pardi Marco e Molari Alessandro ***/
/******************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
/*** KINECT ***/
using Coding4Fun.Kinect.Wpf;
using Microsoft.Kinect;
using Microsoft.Samples.Kinect.WpfViewers;

/*** RIDEFINIZIONI AMBIGUITA' ***/
using MessageBox = System.Windows.MessageBox;
using Nui = Microsoft.Kinect;

/********************************************/
/********************************************/
/********************************************/

namespace MuovereMouse_Windows7___by_Demol
{
    public partial class MainWindow : Window
    {
        /*** COSTANTI ***/
        private const float SogliaSingoloClick = 0.33f;
        private const float SogliaDoppioClick = 0.55f;
        private const float SkeletonMaxX = 0.60f;
        private const float SkeletonMaxY = 0.40f;
        private NotifyIcon IconTray = new NotifyIcon();
        DateTime tempo_inizio = new DateTime();
        DateTime tempo_fine = new DateTime();
        /****************/
        /*** COSTRUTTORE ***/
        public MainWindow()
        {
            InitializeComponent();
        }
        /****************************/
        /*** CARICAMENTO FINESTRA ***/
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (KinectSensor.KinectSensors.Count!=0)
                kinectSensorChooser.KinectSensorChanged += new DependencyPropertyChangedEventHandler(SelettoreKinect_CambioStato);
            else
            {
                MessageBox.Show(
                                "NESSUN KINECT COLLEGATO!! :(\n\n"+
                                "Chiudere il programma, collegare il sensore Kinect e avviare nuovamente.",
                                "FATAL ERROR:",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                                );
                //Disabilito il resto dell'interfaccia, perchè inutile
                MotoreSU.IsEnabled = false;
                MotoreGIU.IsEnabled = false;
                CheckManoSinistra.IsEnabled = false;
                Video.IsEnabled = false;
            }
        }
        void SelettoreKinect_CambioStato(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor old = (KinectSensor)e.OldValue;

            StopKinect(old);

            KinectSensor sensor = (KinectSensor)e.NewValue;

            if (sensor == null) return;

            TransformSmoothParameters parameters = new TransformSmoothParameters();
            parameters.Smoothing = 0.7f;
            parameters.Correction = 0.3f;
            parameters.Prediction = 0.4f;
            parameters.JitterRadius = 1.0f;
            parameters.MaxDeviationRadius = 0.5f;

            sensor.SkeletonStream.Enable(parameters);
            sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);

            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_SetUpFrames);
            try
            {
                sensor.Start();
            }
            catch (System.IO.IOException)
            {
                MessageBox.Show(
                                "Qualche altra applicazione sta usando il Kinect.", 
                                "FATAL ERROR:", 
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                                );
            }
        }
        /****************************/
        /*** SETUP SENSORI ***/
        void sensor_SetUpFrames(object sender, AllFramesReadyEventArgs e)
        {
            sensor_SetUp_Depth(e);
            sensor_SetUp_Skeleton(e);
        }
        void sensor_SetUp_Skeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null) return;

                Skeleton[] allSkeletons = new Skeleton[skeletonFrameData.SkeletonArrayLength];

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                foreach (Skeleton sd in allSkeletons)
                {
                    //il primo "osso" trovato/tracciato muove il cursore del mouse
                    if (sd.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        //ci assicuriamo che entrambe le mani siano tracciate
                        if (sd.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked &&
                            sd.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                        {
                            int cursorX, cursorY;

                            //get Joint di entrambe le mani
                            Joint jointRight = sd.Joints[JointType.HandRight];
                            Joint jointLeft = sd.Joints[JointType.HandLeft];

                            /************************************************************/
                            /*** SCALO LE PROPORZIONI PER ADATTARLE AL MONITOR ATTIVO ***/
                            /************************************************************/
                            Joint scaledRight = jointRight.ScaleTo(
                                                                  (int)SystemParameters.PrimaryScreenWidth, 
                                                                  (int)SystemParameters.PrimaryScreenHeight, 
                                                                  SkeletonMaxX, 
                                                                  SkeletonMaxY
                                                                  );
                            Joint scaledLeft = jointLeft.ScaleTo(
                                                                (int)SystemParameters.PrimaryScreenWidth,
                                                                (int)SystemParameters.PrimaryScreenHeight,
                                                                SkeletonMaxX, 
                                                                SkeletonMaxY
                                                                );
                            /************************************************************/

                            //capire la posizione del cursore in base alla mano sinistra/destra
                            if (CheckManoSinistra.IsChecked.GetValueOrDefault())
                            {
                                cursorX = (int)scaledLeft.Position.X;
                                cursorY = (int)scaledLeft.Position.Y;
                            }
                            else
                            {
                                cursorX = (int)scaledRight.Position.X;
                                cursorY = (int)scaledRight.Position.Y;
                            }

                            /*** VAR ***/
                            bool ClickSinistro;
                            /***********/

                            //capire se il pulsante del mouse va premuto
                            if ((CheckManoSinistra.IsChecked.GetValueOrDefault() && jointRight.Position.Y > SogliaSingoloClick) ||
                                    (!CheckManoSinistra.IsChecked.GetValueOrDefault() && jointLeft.Position.Y > SogliaSingoloClick))
                            {
                                if (jointLeft.Position.Y > SogliaDoppioClick || jointRight.Position.Y > SogliaDoppioClick)
                                {     //se è maggiore della soglia del Doppio, chiamerò il doppio CLICK
                                    GestoreMouse.DoppioClick(
                                                             cursorX,
                                                             cursorY,
                                                             (int)SystemParameters.PrimaryScreenWidth,
                                                             (int)SystemParameters.PrimaryScreenHeight
                                                             );
                                    return;
                                }
                                else
                                {
                                    tempo_inizio = DateTime.Now;    //so quando è partito
                                    ClickSinistro = true;
                                }
                            }   //non è un click, ma è solo lo spostamento del cursore
                            else
                            {
                                ClickSinistro = false;
                            }

                            /*** ESECUZIONE INPUT ***/
                            GestoreMouse.MouseInput(
                                                    cursorX, 
                                                    cursorY, 
                                                    (int)SystemParameters.PrimaryScreenWidth, 
                                                    (int)SystemParameters.PrimaryScreenHeight, 
                                                    ClickSinistro
                                                    );
                            /************************/

                            return;
                        }
                    }
                }
            }
        }
        void sensor_SetUp_Depth(AllFramesReadyEventArgs e)
        {
            //Se la finestra è attiva, visualizzo lo stream video
            if (this.WindowState == WindowState.Normal)
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    if (depthFrame == null) return;
                    Video.Source = depthFrame.ToBitmapSource();
                }
            }
        }
        /*********************/
        /*** CAMBIO STATO FINESTRA ***/
        private void Window_StateChanged(object sender, EventArgs e)
        {
            //Se vuole minimizzare, nascondo dalla barra delle applicazioni
            if (WindowState == WindowState.Minimized)  
                this.Hide();    //--> è riapribile nella TryBar
                /*** TRAY ICON ***/
                IconTray.Icon = new Icon("icon.ico");
                IconTray.Visible = true;
                IconTray.BalloonTipTitle = "Kinect Mouse";
                IconTray.BalloonTipText = "Buon divertimento!!";
                IconTray.Text = "Gestore Kinect";
                IconTray.Click += delegate
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Focus();
                };
        }
        /*****************************/
        /*** CHIUSURA FINESTRA ***/
        private void Window_Closed(object sender, EventArgs e)
        {
            IconTray.Visible = false;
			if (kinectSensorChooser.Kinect != null)
			{
                StopKinect(kinectSensorChooser.Kinect);
			}
        }
        /*************************/
        /*** STOP KINECT ***/
        private void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                if (sensor.IsRunning)
                {
                    try
                    {
                        sensor.Stop();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(
                                        "Impossibile stopppare il kinect!!\n\n"+e.ToString(),
                                        "FATAL ERROR:",
                                        MessageBoxButton.OK
                                        );
                    }
                }
            }
        }
        /*******************/
        /*** CAMBIO ANGOLAZIONE ***/
        private void MotoreSu(object sender, RoutedEventArgs e)
        {
            try
            {
                kinectSensorChooser.Kinect.ElevationAngle += 3;
            }
            /*** ERRORE ***/
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Errore");
            }
            /*** LIMITE RAGGIUNTO ***/
            catch (ArgumentOutOfRangeException outOfRangeException)
            {
                MessageBox.Show("Altezza massima raggiunta!");
            }
        }
        private void MotoreGiu(object sender, RoutedEventArgs e)
        {
            try
            {
                kinectSensorChooser.Kinect.ElevationAngle -= 3;
            }
            /*** ERRORE ***/
            catch (InvalidOperationException ex)
            {
                MessageBox.Show("Errore");
            }
            /*** LIMITE RAGGIUNTO ***/
            catch (ArgumentOutOfRangeException outOfRangeException)
            {
                MessageBox.Show("Altezza massima raggiunta!");
            }
        }
        /**************************/
    }
}
