using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using SysWinForms = System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace BattleShips
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BattleShips : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public string _playerName = "";

        //// Windows forms and associated thingies
        public Form1 _myForm;

        // State
        ConnectionState _connectionState;
        public GameState _gameState;
        Point _mousePosition;
        Point _mousePositionPrevious;
        MouseState _mouseState;
        MouseState _mouseStatePrevious;
        KeyboardState _keyboardState;
        KeyboardState _keyboardStatePrevious;

        // Data
        Point _currentPoint;
        public List<Point> _selectedTiles;
        public List<List<Point>> _boats;
        public List<Point> _currentlySelectedTiles;
        public List<Point> _myHitTiles;
        public List<Point> _targetedTiles;
        public List<Point> _hitTiles;
        public List<Point> _bombedTiles;
        Rectangle _grid1;
        Rectangle _grid2;

        // Connection data
        IPAddress _connectionIp;
        public TCPClient _connectionClient;

        // Resources
        Texture2D _4x4;
        Texture2D _32x32square;
        Texture2D _32x32squareGrey;
        Texture2D _32x32squareGolden;
        SpriteFont _defaultFont;

        public BattleShips()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 10);
            graphics.PreferredBackBufferWidth = 860;
            graphics.PreferredBackBufferHeight = 384;
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            _connectionIp = IPAddress.Loopback;
            //IPAddress.TryParse("46.4.184.109", out _connectionIp);
            _connectionState = ConnectionState.CONNECTING;
            _gameState = GameState.NONE;

            _currentPoint = new Point();

            _mousePosition = new Point(Mouse.GetState().X, Mouse.GetState().Y);
            _mousePositionPrevious = new Point(Mouse.GetState().X, Mouse.GetState().Y);

            _mouseStatePrevious = Mouse.GetState();
            _keyboardStatePrevious = Keyboard.GetState();

            _selectedTiles = new List<Point>();
            _boats = new List<List<Point>>();
            _currentlySelectedTiles = new List<Point>();
            _myHitTiles = new List<Point>();
            _targetedTiles = new List<Point>();
            _hitTiles = new List<Point>();
            _bombedTiles = new List<Point>();

            _grid1 = new Rectangle(32, 32, 320, 320);
            _grid2 = new Rectangle(480, 32, 320, 320);

            base.Initialize();

            SysWinForms.Form gameWindowForm = (SysWinForms.Form)SysWinForms.Form.FromHandle(this.Window.Handle);
            gameWindowForm.Shown += new EventHandler(gameWindowForm_Shown);

            this._myForm = new Form1(this);
            _myForm.HandleDestroyed += new EventHandler(myForm_HandleDestroyed);
            _myForm.Show();

            Mouse.WindowHandle = this._myForm.PanelHandle;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            _4x4 = Content.Load<Texture2D>("4x4");
            _32x32square = Content.Load<Texture2D>("32x32");
            _32x32squareGrey = Content.Load<Texture2D>("32x32_grey");
            _32x32squareGolden = Content.Load<Texture2D>("32x32_golden");
            _defaultFont = Content.Load<SpriteFont>("SpriteFont1");

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.A))
                _connectionClient.SendTargets();
            if (_connectionClient == null)
            {
                _connectionState = ConnectionState.CONNECTING;
            }
            // At the start of the update loop, update mouse and keyboard states
            _mousePosition.X = Mouse.GetState().X;
            _mousePosition.Y = Mouse.GetState().Y;
            _mouseState = Mouse.GetState();
            _keyboardState = Keyboard.GetState();

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                || _keyboardState.IsKeyDown(Keys.Escape))
                this.Exit();

            switch (_connectionState)
            {
                case ConnectionState.CONNECTING:
                    Connect(gameTime);
                    break;
                case ConnectionState.CONNECTED:
                    if (IsActive)
                    {
                        switch (_gameState)
                        {
                            case GameState.NONE:
                                break;
                            default:
                                GameUpdate(gameTime);
                                break;
                        }
                    }
                    break;
                default:
                    this.Exit();
                    break;
            }

            // At the end of the update loop, update the last mouse and keyboard states
            _mouseStatePrevious = Mouse.GetState();
            _keyboardStatePrevious = Keyboard.GetState();
            _mousePositionPrevious.X = Mouse.GetState().X;
            _mousePositionPrevious.Y = Mouse.GetState().Y;
            
            base.Update(gameTime);
        }

        protected void Connect(GameTime gameTime)
        {
            if (_connectionClient == null)
            {
                try
                {
                    _connectionClient = new TCPClient(this, _connectionIp, 7168);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                if (_connectionClient == null)
                    this.Exit();
                else
                    this._connectionState = ConnectionState.CONNECTED;
            }
        }

        protected void GameUpdate(GameTime gameTime)
        {
            Rectangle currentGrid = _grid1;
            if (!_grid1.Contains(_mousePosition))
                currentGrid = _grid2;

            if (currentGrid != null)
            {
                _currentPoint.X = (_mouseState.X - currentGrid.X) / 32;
                _currentPoint.Y = (_mouseState.Y - currentGrid.Y) / 32;
            }

            if (_gameState == GameState.SETBOATS)
            {
                if (_mouseState.LeftButton == ButtonState.Pressed && _mouseStatePrevious.LeftButton == ButtonState.Released)
                {
                    // On press
                    if (_grid1.Contains(_mousePosition))
                    {
                        _currentlySelectedTiles.Add(_currentPoint);
                    }
                }
                else if (_mouseState.LeftButton == ButtonState.Pressed)
                {
                    // While pressed
                    if (_grid1.Contains(_mousePosition))
                    {
                        if (!_currentlySelectedTiles.Contains(_currentPoint))
                        {
                            bool inStraightLine = true;
                            bool nextInLine = false;
                            foreach (Point point in _currentlySelectedTiles)
                            {
                                if (point.X != (_mouseState.X - _grid1.X) / 32 && point.Y != (_mouseState.Y - _grid1.Y) / 32)
                                {
                                    inStraightLine = false;
                                }
                                if (point.X - 1 == (_mouseState.X - _grid1.X) / 32 || point.X + 1 == (_mouseState.X - _grid1.X) / 32
                                    || point.Y - 1 == (_mouseState.Y - _grid1.Y) / 32 || point.Y + 1 == (_mouseState.Y - _grid1.Y) / 32)
                                    nextInLine = true;
                            }

                            if (inStraightLine && !_selectedTiles.Contains(_currentPoint) && nextInLine && _currentlySelectedTiles.Count < (18 - _selectedTiles.Count > 5 ? 5 : 17 - _selectedTiles.Count))
                                _currentlySelectedTiles.Add(_currentPoint);
                        }
                    }
                }
                else if (_mouseState.LeftButton == ButtonState.Released && _mouseStatePrevious.LeftButton == ButtonState.Pressed)
                {
                    // On release
                    if (_grid1.Contains(_mousePosition))
                    {
                        if (_currentlySelectedTiles.Count + _selectedTiles.Count <= 17)
                        {
                            List<Point> newList = new List<Point>();
                            foreach (Point cell in _currentlySelectedTiles)
                            {
                                if (!_selectedTiles.Contains(cell))
                                {
                                    _selectedTiles.Add(cell);
                                    newList.Add(cell);
                                }
                            }
                            _boats.Add(newList);
                        }
                        _currentlySelectedTiles.Clear();
                    }
                }
                else if (_mouseState.LeftButton == ButtonState.Released)
                {
                    // While released
                    if (_currentlySelectedTiles.Count > 0)
                        _currentlySelectedTiles.Clear();
                }

                if (_mouseState.RightButton == ButtonState.Released && _mouseStatePrevious.RightButton == ButtonState.Pressed)
                {
                    // On release
                    if (_grid1.Contains(_mousePosition))
                    {
                        for (int i = 0; i < _boats.Count; i++)
                        {
                            if (_boats[i].Contains(_currentPoint))
                            {
                                for (int j = 0; j < _boats[i].Count; j++)
                                    _selectedTiles.Remove(_boats[i][j]);
                                _boats.Remove(_boats[i]);
                            }
                        }
                    }
                }
            }
            else if (_gameState == GameState.MAKETURN)
            {
                if (_mouseState.LeftButton == ButtonState.Released && _mouseStatePrevious.LeftButton == ButtonState.Pressed)
                {
                    if (_grid2.Contains(_mousePosition))
                    {
                        if (!_targetedTiles.Contains(_currentPoint) && !_bombedTiles.Contains(_currentPoint) && _targetedTiles.Count < 5)
                        {
                            if (_targetedTiles.Contains(_currentPoint))
                                _targetedTiles.Remove(_currentPoint);
                            else
                                _targetedTiles.Add(_currentPoint);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            {
                switch (_connectionState)
                {
                    case ConnectionState.CONNECTING:
                        spriteBatch.DrawString(_defaultFont, "Connecting to server", Vector2.Zero, Color.Black);
                        break;
                    case ConnectionState.CONNECTED:
                        switch (_gameState)
                        {
                            case GameState.NONE:
                                spriteBatch.DrawString(_defaultFont, "Welcome to XNA NetBattleships.", new Vector2(25, 25), Color.Black);
                                spriteBatch.DrawString(_defaultFont, "To chat, type a message in the box below and press return.", new Vector2(25, 75), Color.Black);
                                spriteBatch.DrawString(_defaultFont, "To challenge somebody to a game or accept a challenge,", new Vector2(25, 125), Color.Black);
                                spriteBatch.DrawString(_defaultFont, "select their name to the right and press Challenge!", new Vector2(25, 150), Color.Black);
                                spriteBatch.DrawString(_defaultFont, "Good luck and have fun!", new Vector2(25, 250), Color.Black);
                                break;
                            default:
                                DrawGame(gameTime);
                                break;
                        }
                        break;
                }
            }
            spriteBatch.End();

            base.Draw(gameTime);
            this.GraphicsDevice.Present(null, null, this._myForm.PanelHandle);
        }

        protected void DrawGame(GameTime gameTime)
        {
            // TODO: Add your drawing code here
            if (_mouseState.LeftButton == ButtonState.Released && _grid1.Contains(_mousePosition) || _grid2.Contains(_mousePosition))
            {
                spriteBatch.Draw(_32x32squareGolden, new Vector2(32 * (_mouseState.X / 32), 32 * (_mouseState.Y / 32)), Color.White);
            }

            foreach (Point point in _selectedTiles)
            {
                spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid1.X, _grid1.Y), Color.Black);
            }

            foreach (Point point in _bombedTiles)
            {
                spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid2.X, _grid2.Y), Color.Black);
            }

            foreach (Point point in _hitTiles)
            {
                spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid2.X, _grid2.Y), Color.Red);
            }

            if (_grid1.Contains(_mousePosition))
            {
                foreach (Point point in _currentlySelectedTiles)
                {
                    spriteBatch.Draw(_32x32squareGrey, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid1.X, _grid1.Y), Color.White);
                }
            }

            foreach (Point point in _targetedTiles)
            {
                spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid2.X, _grid2.Y), Color.Green);
            }

            if (_mouseState.LeftButton == ButtonState.Released && _selectedTiles.Contains(_currentPoint) && _grid1.Contains(_mousePosition))
            {
                foreach (List<Point> boat in _boats)
                {
                    if (boat.Contains(_currentPoint))
                    {
                        foreach (Point point in boat)
                        {
                            spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid1.X, _grid1.Y), Color.White);
                        }
                    }
                }
            }

            foreach (Point point in _myHitTiles)
            {
                if (_selectedTiles.Contains(point))
                    spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid1.X, _grid1.Y), Color.Red);
                else
                    spriteBatch.Draw(_32x32square, (new Vector2(point.X, point.Y) * 32) + new Vector2(_grid1.X, _grid1.Y), Color.Green);
            }

            DrawGrid(new Vector2(_grid1.X, _grid2.Y), new Vector2(10, 10), 32);
            DrawGrid(new Vector2(_grid2.X, _grid2.Y), new Vector2(10, 10), 32);
        }

        protected void DrawLine(Vector2 point1, Vector2 point2)
        {
            Vector2 line = point2 - point1;
            float rotation = -(float)(Math.Atan2(line.X, line.Y) - MathHelper.PiOver2);
            float length = line.Length();

            spriteBatch.Draw(_4x4, point1, null, Color.White, rotation, Vector2.Zero, new Vector2(length / _4x4.Width, 1), SpriteEffects.None, 0);
        }

        protected void DrawGrid(Vector2 topLeft, Vector2 size, int cellWidth)
        {
            for (int x = (int)topLeft.X; x <= topLeft.X + (cellWidth * size.X); x += cellWidth)
            {
                DrawLine(new Vector2(x, topLeft.Y), new Vector2(x, topLeft.Y + (size.Y * cellWidth)));
            }

            for (int y = (int)topLeft.Y; y <= topLeft.Y + (cellWidth * size.Y); y += cellWidth)
            {
                DrawLine(new Vector2(topLeft.X, y), new Vector2(topLeft.X + (size.X * cellWidth), y));
            }
        }

        public void SendChatMessage(string message)
        {
            if (_connectionClient != null)
            {
                byte[] data = new byte[5 + message.Length];
                data[0] = 6;
                Array.Copy(BitConverter.GetBytes(message.Length), 0, data, 1, 4);
                Array.Copy(ASCIIEncoding.ASCII.GetBytes(message), 0, data, 5, message.Length);
                _connectionClient.SendData(data);
            }
        }

        public void ResetGame()
        {
            _gameState = GameState.NONE;
            _selectedTiles.Clear();
            _currentlySelectedTiles.Clear();
            _targetedTiles.Clear();
            _myHitTiles.Clear();
            _bombedTiles.Clear();
            _hitTiles.Clear();
            _targetedTiles.Clear();
            _myForm.ToggleButtons(true);
            _myForm.DisableInterface(false);
            _myForm.ShowButtons(false);
        }

        void myForm_HandleDestroyed(object sender, EventArgs e)
        {
            this.Exit();
        }

        void gameWindowForm_Shown(object sender, EventArgs e)
        {
            ((SysWinForms.Form)sender).Hide();
        }
    }

    public enum ConnectionState
    {
        NONE,
        CONNECTING,
        CONNECTED,
        DISCONNECTED
    }

    public enum GameState
    {
        NONE,
        SETBOATS,
        MAKETURN,
        WAIT
    }
}
