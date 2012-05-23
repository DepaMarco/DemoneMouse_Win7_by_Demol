#pragma warning disable 0649

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MuovereMouse_Windows7___by_Demol
{
    /**************/
    /*** STRUCT ***/
    /**************/
    internal struct MouseInput
	{
		public int X;
		public int Y;
		public uint MouseData;
		public uint Flags;
		public uint Time;
		public IntPtr ExtraInfo;
	}
	internal struct Input
	{
		public int Tipo;
		public MouseInput MouseInput;
	}
    /**************/

    /***************/
    /*** GESTORE ***/
    /***************/
    public static class GestoreMouse
    {
        public const int InputMouse = 0;

        public const int MouseEventMove = 0x01;
        public const int MouseEventLeftDown = 0x02;
        public const int MouseEventLeftUp = 0x04;
        public const int MouseEventRightDown = 0x08;
        public const int MouseEventRightUp = 0x10;
        public const int MouseEventAbsolute = 0x8000;

        private static bool lastLeftDown;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint numInputs, Input[] inputs, int size);

        /*****************************************************/
        /*** INPUT DEL MOUSE: SPOSTAMENTO O CLICK SINISTRO ***/
        /*****************************************************/
        public static void MouseInput(int positionX, int positionY, int maxX, int maxY, bool leftDown)
        {
            if (positionX > int.MaxValue)
                throw new ArgumentOutOfRangeException("positionX");
            if (positionY > int.MaxValue)
                throw new ArgumentOutOfRangeException("positionY");

            Input[] i = new Input[2];

            //sposto il mouse nella posizione specificata
            i[0] = new Input();
            i[0].Tipo = InputMouse;
            i[0].MouseInput.X = (positionX * 65535) / maxX;
            i[0].MouseInput.Y = (positionY * 65535) / maxY;
            i[0].MouseInput.Flags = MouseEventAbsolute | MouseEventMove;

            //determino se l'evento è "bottone giù" o "bottone su" del mouse
            if (!lastLeftDown && leftDown)
            {
                i[1] = new Input();
                i[1].Tipo = InputMouse;
                i[1].MouseInput.Flags = MouseEventLeftDown;
            }
            else if (lastLeftDown && !leftDown)
            {
                i[1] = new Input();
                i[1].Tipo = InputMouse;
                i[1].MouseInput.Flags = MouseEventLeftUp;
            }

            lastLeftDown = leftDown;

            /*** ESECUZIONE INPUT ***/
            uint result = SendInput(2, i, Marshal.SizeOf(i[0]));
            if (result == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            else //forzo l'esecuzione dell'evento "ClickSu"
            {
                //Senza di questo input, i click sui controlli della finestra non vanno
                Input[] bis = new Input[1];
                bis[0].Tipo = InputMouse;
                bis[0].MouseInput.Flags = MouseEventLeftUp;
                result = SendInput(1, bis, Marshal.SizeOf(bis[0]));
            }
        }
        /*****************************************************/

        /********************/
        /*** DOPPIO CLICK ***/
        /********************/
        public static void DoppioClick(int positionX, int positionY, int maxX, int maxY)
        {
            uint result;
            Input[] doppio = new Input[4];
            //set parametri dell'input
            //1
            doppio[0].Tipo = InputMouse;
            doppio[0].MouseInput.Flags = MouseEventLeftDown;
            doppio[1].Tipo = InputMouse;
            doppio[1].MouseInput.Flags = MouseEventLeftUp;
            //2
            doppio[2].Tipo = InputMouse;
            doppio[2].MouseInput.Flags = MouseEventLeftDown;
            doppio[3].Tipo = InputMouse;
            doppio[3].MouseInput.Flags = MouseEventLeftUp;
            //esecuzione
            result = SendInput(4, doppio, Marshal.SizeOf(doppio[0]));
            //controllo esecuzione
            if (result == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        /********************/
    }
    /***************/
}
