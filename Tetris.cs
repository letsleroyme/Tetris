
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Tetris
{
    /// <summary>
    /// Этот класс описывает площадку для тетриса и основные методы работы с фигурами
    /// </summary>
    class Tetris
    {
        /// <summary>
        /// ссколько пикселей в 1м блоке
        /// </summary>
        public const int K = 6;
        /// <summary>
        /// Список уже упавших фигур
        /// </summary>
        public List<Brick> StaticBrickList;
        /// <summary>
        /// Это поля ссылаются на панели, где мы будем отображать игру.
        /// </summary>
        Panel GField, NFigure;
        /// <summary>
        /// тута ссылка на то, куда рисовать счет
        /// </summary>
        Label ScoreLabel;
        /// <summary>
        /// На всякий случай было бы неплохо храниться ссылку на поток игры.
        /// </summary>
        Thread gameThread;
        /// <summary>
        /// размеры поля для игры
        /// </summary>
        int height, width;
        /// <summary>
        /// Ссылка на графику панелей.
        /// </summary>
        Graphics grapGame, graphNext;
        /// <summary>
        /// хранятсья текущие и следующие фигуры
        /// </summary>
        Figure curFig, nextFig;
        /// <summary>
        /// объект для синхронизации обращений к перериисовки (нельзя из разных потоков рисовать без синхронизации)
        /// </summary> 
        public static string SYNK = "s";
        /// <summary>
        /// что бы не перерисовывать сетку заного каждый раз, мы сделаем bitmap сетку и будем ее проецировать на игру
        /// </summary>
        Bitmap grid;
        /// <summary>
        /// Этот генератор псевдослучайных числе мы будем испоьзовать для выбора следующей фигуры
        /// </summary>
        Random random = new Random(DateTime.Now.Second);
        /// <summary>
        /// Счёт
        /// </summary>
        int Score;
        /// <summary>
        /// Флаг, по которому выходить из игры
        /// </summary>
        bool Gaming;

        public Tetris(Panel gameField, Panel nextFigure, Label scr)
        {
            ScoreLabel = scr;
            GField = gameField;
            NFigure = nextFigure;
            height = GField.Height;
            width = GField.Width;
            grapGame = GField.CreateGraphics();
            graphNext = NFigure.CreateGraphics();

            grid = new Bitmap(240, 480);
            Graphics gr = Graphics.FromImage(grid);
            //рисуем сетку на Bitmap
            {
                for (int i = 1; i < 21; i++)
                {
                    gr.DrawLine(Pens.Gray, 0, i * K * 4, width, i * K * 4);
                }
                for (int i = 1; i < 11; i++)
                {
                    gr.DrawLine(Pens.Gray, i * K * 4, 0, i * K * 4, height);
                }

            }

        }
        /// <summary>
        /// этот метод выбирает случайную фигуру
        /// </summary>
        void NextFig()
        {
            switch (random.Next(0, 7))
            {
                case 0:
                    nextFig = new SquardFigure();
                    break;
                case 1:
                    nextFig = new T_Figure();
                    break;
                case 2:
                    nextFig = new Z_Figure_left();
                    break;
                case 3:
                    nextFig = new Z_Figure_right();
                    break;
                case 4:
                    nextFig = new G_Figure_Right();
                    break;
                case 5:
                    nextFig = new Line_Figure();
                    break;
                case 6:
                case 7:
                    nextFig = new G_Figure_Left();
                    break;
            }
            graphNext.FillRectangle(Brushes.White, 0, 0, 100, 100);//очищаем холст
            nextFig.DrawAsNextFig(graphNext);//рисуем следующую фигуру
        }
        /// <summary>
        /// Запуск потока игры
        /// </summary>
        public void Start()
        {
            NextFig();
            curFig = nextFig;
            StaticBrickList = new List<Brick>();
            Score = 0;
            ScoreLabel.Text = "Счёт: " + Score;
            Gaming = true;
            gameThread = new Thread(GameThread);
            gameThread.Start();
            gameThread.IsBackground = true;//Что бы поток завершался при выходе из игры
        }
        /// <summary>
        /// Этот метод выполняется в фоновом потоке. Он отвечает за гравитацию и проверку косания
        /// </summary>
        void GameThread()
        {
            Thread.Sleep(100);
            NextFig();
            while (Gaming)
            {
                StartGAmeThread://метка
                if (curFig.Y + curFig.Height >= 80)//косаемся дна?
                {
                    curFig.UpdateBrics();
                    StaticBrickList.Add(curFig.Br1);
                    StaticBrickList.Add(curFig.Br2);
                    StaticBrickList.Add(curFig.Br3);
                    StaticBrickList.Add(curFig.Br4);

                    curFig = nextFig;
                    NextFig();
                    CheckLine();
                    goto StartGAmeThread;//возвращаемся назад.
                }
                else
                {
                    foreach (Brick f in StaticBrickList)//проверяем всех, с кем косаемся
                    {
                        //если хотябы один из 4х блоков фигуры косаеться другой, то нужно оставить эту фигурку и получить новую
                        if (curFig.Br1.BottomCollision(f) || curFig.Br2.BottomCollision(f) || curFig.Br3.BottomCollision(f) || curFig.Br4.BottomCollision(f))
                        {
                            if (curFig.Y == 0)//если фигура села на мель уже сразу после старта, то игра окончена
                            {
                                Gaming = false;
                                goto EndThread;//выходим из цикла
                            }
                            curFig.UpdateBrics();
                            StaticBrickList.Add(curFig.Br1);
                            StaticBrickList.Add(curFig.Br2);
                            StaticBrickList.Add(curFig.Br3);
                            StaticBrickList.Add(curFig.Br4);
                            curFig = nextFig;
                            NextFig();
                            CheckLine();
                            goto StartGAmeThread;//возвращаемся назад.
                        }
                    }
                }
                curFig.Y+=4;//спускаемся
                curFig.UpdateBrics();//обновляем кирпичики
                Redraw();
                Thread.Sleep(500);
                System.GC.Collect();//на всякий пожарный)))
            }
            EndThread://метка
            Redraw();
            grapGame.DrawString("игра окончена", new Font("Areal", 20), Brushes.Black, 120 - grapGame.MeasureString("игра окончена", new Font("Areal", 20)).Width / 2, 230);
            grapGame.DrawString("нажмите любую клавишу, что бы начать заного", new Font("Areal", 7.5f), Brushes.Black, 120 - grapGame.MeasureString("нажмите любую клавишу, что бы начать заного", new Font("Areal", 7.5f)).Width / 2, 260);

        }
        /// <summary>
        /// Эта процедура проверяет наличае заполненных рядов и производит их удаление
        /// </summary>
        void CheckLine()
        {
            for (int i = 0; i < 20; i++)
            {
                List<Brick> line = StaticBrickList.FindAll(delegate (Brick b) { return b.Y / 4 == i; });//мы получаем список всех блоков, находящихся на определенных позициях

                if (line.Count == 10)//Чтобы заполнить ряд нужно поставить ровно 10 блоков.
                {
                    StaticBrickList.RemoveAll(delegate (Brick b) { return b.Y / 4 == i; });//удаляем сами блоки
                    List<Brick> upline = StaticBrickList.FindAll(delegate (Brick b) { return b.Y / 4 < i; });//получаем список всех блоков, что выше
                    foreach (Brick b in upline) { b.Y += 4; }//опускаем
                    Score++;//счет++
                    ScoreLabel.Invoke(new Action(() => { ScoreLabel.Text = "Счёт: " + Score; }));//используя делегат мы изменили Label не из родного потока
                }
            }
        }

        /// <summary>
        /// обновляем экран в отдельном потоке
        /// </summary>
        void Redraw()
        {
            lock (SYNK)
            {
                grapGame.FillRectangle(Brushes.White, 0, 0, width, height);
                curFig.Draw(grapGame);
                foreach (Brick f in StaticBrickList) f.Draw(grapGame);
                grapGame.DrawImage(grid, 0, 0);
            }
        }

        /// <summary>
        /// Мы передадим из окна в "тетрис" событие нажатия и здесь обработаем
        /// </summary>
        /// <param name="ev"> Параметры события нажатия </param>
        public void OnKeyDown(KeyEventArgs ev)
        {
            if (Gaming)//если игра идет, то клавиши нужно обработать
            {
                switch (ev.KeyCode)
                {
                    case Keys.D:
                        if (curFig.X + curFig.Width < 40)
                        {
                            //     Console.WriteLine("turn Right");
                            foreach (Brick f in StaticBrickList)
                            {
                                if (curFig.Br1.RightCollision(f) || curFig.Br2.RightCollision(f) || curFig.Br3.RightCollision(f) || curFig.Br4.RightCollision(f))
                                { return; }//если косаемся с фигурой, то подвинуться не можем.
                            }
                            curFig.X++;
                            curFig.UpdateBrics();
                            Redraw();
                        }
                        break;
                    case Keys.A:
                        if (curFig.X > 0)
                        {
                            //  Console.WriteLine("turn Left");
                            foreach (Brick f in StaticBrickList)
                            {
                                if (curFig.Br1.LeftCollision(f) || curFig.Br2.LeftCollision(f) || curFig.Br3.LeftCollision(f) || curFig.Br4.LeftCollision(f))
                                {
                                    return;//если косаемся с фигурой, то подвинуться не можем.
                                }
                            }
                            curFig.X--;
                            curFig.UpdateBrics();
                            Redraw();
                        }
                        break;
                    case Keys.W:
                        List<Brick> colB = StaticBrickList.FindAll(delegate (Brick b) { return b.Y < curFig.Y + curFig.Width && b.Y + 4 > curFig.Y && b.X + 4 > curFig.X && b.X < curFig.X + curFig.Height; });//мы должны посмотреть, будет ли фиггура косаться других фигур после поворота. Тк поворт происходит на 90 градусов, высота и ширина меняются местами

                        if (curFig.Y + curFig.Width >= 80 || colB.Count > 0) { return; }//если фигура посе поворота косаеться дна или другой фигуры, то поворот блокируется
                        curFig.RotateIncrement();
                        Redraw();
                        break;
                    case Keys.Space:

                        List<Brick> brics = StaticBrickList.FindAll(delegate (Brick b) { return b.Y > curFig.Y + curFig.Height && curFig.X + curFig.Width > b.X && b.X + 4 > curFig.X; });//список всех фигур, что по фигурой игрока
                        if (brics.Count > 0)
                        {
                            Brick higest = brics[0];
                            foreach (Brick b in brics)//ищем самый высокий
                            {
                                if (b.Y < higest.Y) { higest = b; }
                            }
                            if ((byte)(higest.Y - curFig.Height - 4) > curFig.Y)//если фигура и так близко к самой высокой фигуре, то бриближать ее не надо
                            {
                                curFig.Y = (byte)(higest.Y - curFig.Height -4);
                            }
                        }
                        else
                        {
                            curFig.Y = (byte)(80 - curFig.Height);
                        }

                        break;
                }
            }
            else { Start(); }//Если игра закончена, то нужно запустить ее

        }
    }
    /// <summary>
    /// Этот класс описывает один блок. (Каждая фигура состоит ровно из четырех блоков)
    /// </summary>
    class Brick
    {
        public byte X, Y;
        Brush brush;
        public Brick(Brush b) { brush = b; }
        /// <summary>
        /// Рисуем
        /// </summary>
        /// <param name="g"></param>
        public void Draw(Graphics g)
        {
            g.FillRectangle(brush, X * Tetris.K, Y * Tetris.K, 4 * Tetris.K, 4 * Tetris.K);
        }
        /// <summary>
        /// Косается ли дно данного блока с блоком из параметра
        /// </summary>
        /// <param name="br">с кем косаеться</param>
        /// <returns>Косаеться ли</returns>
        public bool BottomCollision(Brick br)
        {
            if (Y < br.Y + 4 && X + 4 > br.X && X < br.X + 4 && Y + 4 >= br.Y) { return true; }
            return false;
        }
        public bool RightCollision(Brick br)
        {
            if (br.X + 4 > X + 4 && X + 4 >= br.X && Y + 4 > br.Y && br.Y + 4 > Y) { return true; }
            return false;
        }
        public bool LeftCollision(Brick br)
        {
            if (br.X < X && X <= br.X + 4 && Y + 4 > br.Y && br.Y + 4 > Y) { return true; }
            return false;
        }
    }

    /// <summary>
    /// Этот класс описывает фигуры (позиция, размер, цвет). Для каждой фигуры будет создан свой класс, наследуемый из этого.
    /// </summary>
    abstract class Figure
    {
        /// <summary>
        /// основные характиристики фигуры: позиция, вращение и размер.
        /// </summary>
        public byte X, Y, Width, Height, Rotate;
        /// <summary>
        /// У каждой фигуры ровно 4 блока, из которых она состоит. Эти блоки описаны классом Brick
        /// </summary>
        public Brick Br1, Br2, Br3, Br4;

        /// <summary>
        /// Этот метод ресует себя на graphic
        /// </summary>
        /// <param name="g"> То, куда нарисоваться </param>
        public void Draw(Graphics g)
        {
            Br1.Draw(g);
            Br2.Draw(g);
            Br3.Draw(g);
            Br4.Draw(g);
        }
        /// <summary>
        /// этот метод обновляет координаты блоков относительно координат фигуры
        /// </summary>
        abstract public void UpdateBrics();


        /// <summary>
        /// В этом методе мы выведем данную фигуру в окошко "Следующая фигура"
        /// </summary>
        /// <param name="g">Grapics панели "следующая фигура"</param>
        public abstract void DrawAsNextFig(Graphics g);
        /// <summary>
        /// повернуть и перерисовать фигуру
        /// </summary>
        public virtual void RotateIncrement()
        {
            Rotate++;
            if (Rotate > 3) Rotate = 0;
            UpdateBrics();
            if (X + Width > 40) { X = (byte)(40 - Width); }
        }
    }

    class SquardFigure : Figure
    {
        public SquardFigure()
        {
            Height = 8;
            Width = 8;
            X = 16;
            Y = 0;
            Br1 = new Brick(Brushes.Red);
            Br2 = new Brick(Brushes.Red);
            Br3 = new Brick(Brushes.Red);
            Br4 = new Brick(Brushes.Red);
            Rotate = 0;
            UpdateBrics();
        }

        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Red, 25, 25, 50, 50);
        }

        public override void UpdateBrics()
        {
            Br1.X = X;
            Br1.Y = Y;
            Br2.X = (byte)(X + 4);
            Br2.Y = Y;
            Br3.X = X;
            Br3.Y = (byte)(Y + 4);
            Br4.Y = (byte)(Y + 4);
            Br4.X = (byte)(X + 4);
        }
    }
    class T_Figure : Figure
    {
        public T_Figure()
        {
            Height = 8;
            Width = 12;
            X = 14;
            Y = 0;
            Br1 = new Brick(Brushes.Yellow);
            Br2 = new Brick(Brushes.Yellow);
            Br3 = new Brick(Brushes.Yellow);
            Br4 = new Brick(Brushes.Yellow);
            Rotate = 0;
            UpdateBrics();
        }
        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Yellow, 12.5f, 25, 75, 25);
            g.FillRectangle(Brushes.Yellow, 37.5f, 50, 25, 25);
        }



        public override void UpdateBrics()
        {
            switch (Rotate)
            {
                case 0:
                    Height = 8;
                    Width = 12;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 8);
                    Br3.Y = Y;
                    Br4.X = (byte)(X + 4);
                    Br4.Y = (byte)(Y + 4);

                    break;
                case 1:
                    Height = 12;
                    Width = 8;
                    Br1.X = (byte)(X + 4);
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = (byte)(X + 4);
                    Br3.Y = (byte)(Y + 8);
                    Br4.X = X;
                    Br4.Y = (byte)(Y + 4);
                    break;
                case 2:
                    Height = 8;
                    Width = 12;
                    Br1.X = X;
                    Br1.Y = (byte)(Y + 4);
                    Br2.X = (byte)(X + 4);
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = (byte)(X + 8);
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = (byte)(X + 4);
                    Br4.Y = Y;
                    break;
                case 3:
                    Height = 12;
                    Width = 8;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = X;
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = X;
                    Br3.Y = (byte)(Y + 8);
                    Br4.X = (byte)(X + 4);
                    Br4.Y = (byte)(Y + 4);
                    break;
            }

        }
    }

    class Z_Figure_left : Figure
    {
        public Z_Figure_left()
        {
            Height = 8;
            Width = 12;
            X = 14;
            Y = 0;
            Br1 = new Brick(Brushes.Green);
            Br2 = new Brick(Brushes.Green);
            Br3 = new Brick(Brushes.Green);
            Br4 = new Brick(Brushes.Green);
            Rotate = 0;
            UpdateBrics();
        }
        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Green, 12.5f, 25, 50, 25);
            g.FillRectangle(Brushes.Green, 37.5f, 50, 50, 25);
        }

        public override void UpdateBrics()
        {
            switch (Rotate)
            {
                case 0:
                case 2:
                    Height = 8;
                    Width = 12;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 8);
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = (byte)(X + 4);
                    Br4.Y = (byte)(Y + 4);

                    break;
                case 1:
                case 3:
                    Height = 12;
                    Width = 8;
                    Br1.X = (byte)(X + 4);
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = X;
                    Br3.Y = (byte)(Y + 8);
                    Br4.X = X;
                    Br4.Y = (byte)(Y + 4);
                    break;

            }
        }
    }
    class Z_Figure_right : Figure
    {
        public Z_Figure_right()
        {
            Height = 8;
            Width = 12;
            X = 14;
            Y = 0;
            Br1 = new Brick(Brushes.Green);
            Br2 = new Brick(Brushes.Green);
            Br3 = new Brick(Brushes.Green);
            Br4 = new Brick(Brushes.Green);
            Rotate = 0;
            UpdateBrics();
        }
        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Green, 37.5f, 25, 50, 25);
            g.FillRectangle(Brushes.Green, 12.5f, 50, 50, 25);
        }

        public override void UpdateBrics()
        {
            switch (Rotate)
            {
                case 0:
                case 2:
                    Height = 8;
                    Width = 12;
                    Br1.X = X;
                    Br1.Y = (byte)(Y + 4);
                    Br2.X = (byte)(X + 4);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 8);
                    Br3.Y = Y;
                    Br4.X = (byte)(X + 4);
                    Br4.Y = (byte)(Y + 4);

                    break;
                case 1:
                case 3:
                    Height = 12;
                    Width = 8;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = (byte)(X + 4);
                    Br3.Y = (byte)(Y + 8);
                    Br4.X = X;
                    Br4.Y = (byte)(Y + 4);
                    break;

            }
        }
    }
    class Line_Figure : Figure
    {
        public Line_Figure()
        {
            Height = 4;
            Width = 16;
            X = 14;
            Y = 0;
            Br1 = new Brick(Brushes.Blue);
            Br2 = new Brick(Brushes.Blue);
            Br3 = new Brick(Brushes.Blue);
            Br4 = new Brick(Brushes.Blue);
            Rotate = 0;
            UpdateBrics();
        }
        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Blue, 0, 37.5f, 100, 25);
        }

        public override void UpdateBrics()
        {
            switch (Rotate)
            {
                case 0:
                case 2:
                    Height = 4;
                    Width = 16;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 8);
                    Br3.Y = Y;
                    Br4.X = (byte)(X + 12);
                    Br4.Y = Y;

                    break;
                case 1:
                case 3:
                    Height = 16;
                    Width = 4;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = X;
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = X;
                    Br3.Y = (byte)(Y + 8);
                    Br4.X = X;
                    Br4.Y = (byte)(Y + 12);
                    break;

            }
        }
    }
    class G_Figure_Left : Figure
    {
        public G_Figure_Left()
        {
            Height = 12;
            Width = 8;
            X = 16;
            Y = 0;
            Br1 = new Brick(Brushes.Magenta);
            Br2 = new Brick(Brushes.Magenta);
            Br3 = new Brick(Brushes.Magenta);
            Br4 = new Brick(Brushes.Magenta);
            Rotate = 0;
            UpdateBrics();
        }

        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Magenta, 50, 12.5f, 25, 75);
            g.FillRectangle(Brushes.Magenta, 25, 12.5f, 25, 25);
        }

        public override void UpdateBrics()
        {
            switch (Rotate)
            {
                case 0:
                    Height = 12;
                    Width = 8;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 4);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 4);
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = (byte)(X + 4);
                    Br4.Y = (byte)(Y + 8);
                    break;
                case 1:
                    Height = 8;
                    Width = 12;

                    Br1.X = X;
                    Br1.Y = (byte)(Y + 4);
                    Br2.X = (byte)(X + 8);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 4);
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = (byte)(X + 8);
                    Br4.Y = (byte)(Y + 4);

                    break;
                case 2:
                    Height = 12;
                    Width = 8;
                    Br1.X = (byte)(X + 4);
                    Br1.Y = (byte)(Y + 8);
                    Br2.X = X;
                    Br2.Y = Y;
                    Br3.X = X;
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = X;
                    Br4.Y = (byte)(Y + 8);
                    break;
                case 3:
                    Height = 8;
                    Width = 12;
                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = X;
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = (byte)(X + 4);
                    Br3.Y = Y;
                    Br4.X = (byte)(X + 8);
                    Br4.Y = Y;
                    break;

            }
        }
    }
    class G_Figure_Right : Figure
    {
        public G_Figure_Right()
        {
            Height = 12;
            Width = 8;
            X = 16;
            Y = 0;
            Br1 = new Brick(Brushes.Magenta);
            Br2 = new Brick(Brushes.Magenta);
            Br3 = new Brick(Brushes.Magenta);
            Br4 = new Brick(Brushes.Magenta);
            Rotate = 0;
            UpdateBrics();
        }

        public override void DrawAsNextFig(Graphics g)
        {
            g.FillRectangle(Brushes.Magenta, 25, 12.5f, 25, 75);
            g.FillRectangle(Brushes.Magenta, 50, 12.5f, 25, 25);
        }

        public override void UpdateBrics()
        {
            switch (Rotate)
            {
                case 0:
                    Height = 12;
                    Width = 8;
                    Br1.X = (byte)(X + 4);
                    Br1.Y = Y;
                    Br2.X = X;
                    Br2.Y = Y;
                    Br3.X = X;
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = X;
                    Br4.Y = (byte)(Y + 8);
                    break;
                case 1:
                    Height = 8;
                    Width = 12;

                    Br1.X = X;
                    Br1.Y = Y;
                    Br2.X = (byte)(X + 8);
                    Br2.Y = (byte)(Y + 4);
                    Br3.X = (byte)(X + 4);
                    Br3.Y = Y;
                    Br4.X = (byte)(X + 8);
                    Br4.Y = Y;

                    break;
                case 2:
                    Height = 12;
                    Width = 8;
                    Br1.X = X;
                    Br1.Y = (byte)(Y + 8);
                    Br2.X = (byte)(X + 4);
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 4);
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = (byte)(X + 4);
                    Br4.Y = (byte)(Y + 8);
                    break;
                case 3:
                    Height = 8;
                    Width = 12;
                    Br1.X = X;
                    Br1.Y = (byte)(Y + 4);
                    Br2.X = X;
                    Br2.Y = Y;
                    Br3.X = (byte)(X + 4);
                    Br3.Y = (byte)(Y + 4);
                    Br4.X = (byte)(X + 8);
                    Br4.Y = (byte)(Y + 4);
                    break;

            }
        }
    }
}
