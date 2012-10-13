﻿using System;
using System.Collections.Generic;
using System.Text;
using Phantom.Core;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Phantom.Misc
{
    public delegate void ConsoleAction(string[] argv);

    public class KonsoulSettings
    {
        public Color BackgroundColor = Color.Black;
        public Color Color = Color.White;
        public float Alpha = 0.8f;
        public int LineCount = 12;
        public float Padding = 4;
        public string Prompt = "] ";
        public Keys OpenKey = Keys.OemTilde;
    }
    public class Konsoul : Component
    {
        /**
         * Needed to receive debug output. (from `Debug.WriteLine' etc)
         */
        private class DebugListener : TraceListener
        {
            private Konsoul console;
            public DebugListener( Konsoul console )
            {
                this.console = console;
            }
            public override void Write(string message)
            {
                this.console.Write(message);
            }

            public override void WriteLine(string message)
            {
                this.console.WriteLine(message);
            }
        }

        public bool Visible;

        private float blinkTimer;

        private KonsoulSettings settings;

        private KeyboardState previousKeyboardState;
        private KeyMap keyMap;

        private readonly DebugListener listener;
        private SpriteFont font;
        private SpriteBatch batch;
        private BasicEffect effect;

        private int scrollOffset;
        private string input;
        private float promptWidth;
        private int cursor;
        private List<string> lines;
        private List<string> wrapBuffer;
        private string nolineBuffer;

        private VertexBuffer backgroundBuffer;
        private VertexBuffer cursorBuffer;
        private IndexBuffer backgroundIndex;

        public Konsoul(SpriteFont font, KonsoulSettings settings)
        {
            this.Visible = false;
            this.font = font;
            this.settings = settings;
            this.batch = new SpriteBatch(PhantomGame.Game.GraphicsDevice);
            this.effect = new BasicEffect(PhantomGame.Game.GraphicsDevice);

            this.input = "";
            this.cursor = 0;
            this.lines = new List<string>();
            this.promptWidth = this.font.MeasureString(this.settings.Prompt).X;
            this.wrapBuffer = new List<string>();
            this.nolineBuffer = "";
            this.scrollOffset = 0;

            this.keyMap = new KeyMap();
            this.previousKeyboardState = Keyboard.GetState();

            Debug.Listeners.Add(this.listener=new DebugListener(this));
            this.SetupVertices();
            this.lines.Add("] Konsoul initialized");
        }

        public Konsoul(SpriteFont font)
            : this(font, new KonsoulSettings())
        {
        }

        private void SetupVertices()
        {
            VertexPositionColor[] vertices = new VertexPositionColor[] {
                new VertexPositionColor(new Vector3(0,0,0), Color.White),
                new VertexPositionColor(new Vector3(1,0,0), Color.White),
                new VertexPositionColor(new Vector3(0,1,0), Color.White),
                new VertexPositionColor(new Vector3(1,1,0), Color.White),
            };
            short[] indices = new short[] { 0, 1, 2, 2, 1, 3 };
            this.backgroundBuffer = new VertexBuffer(PhantomGame.Game.GraphicsDevice, VertexPositionColor.VertexDeclaration, 4, BufferUsage.None);
            this.backgroundBuffer.SetData<VertexPositionColor>(vertices);
            this.backgroundIndex = new IndexBuffer(PhantomGame.Game.GraphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.None);
            this.backgroundIndex.SetData<short>(indices);

            VertexPositionColor[] cursor = new VertexPositionColor[] {
                new VertexPositionColor(new Vector3(0,0,0), Color.White),
                new VertexPositionColor(new Vector3(0,this.font.LineSpacing,0), Color.White),
            };
            this.cursorBuffer = new VertexBuffer(PhantomGame.Game.GraphicsDevice, VertexPositionColor.VertexDeclaration, 2, BufferUsage.None);
            this.cursorBuffer.SetData<VertexPositionColor>(cursor);

        }

        public override void Dispose()
        {
            Debug.Listeners.Remove(this.listener);
            base.Dispose();
        }

        public override void Update(float elapsed)
        {
            this.blinkTimer += elapsed;

            KeyboardState current = Keyboard.GetState();
            KeyboardState previous = this.previousKeyboardState;

            // Open and close logics:
            if (!this.Visible)
            {
                if (current.IsKeyDown(this.settings.OpenKey) && !previous.IsKeyDown(this.settings.OpenKey))
                    this.Visible = true;
                this.previousKeyboardState = current;
                base.Update(elapsed);
                return;
            }
            else if ((current.IsKeyDown(this.settings.OpenKey) && !previous.IsKeyDown(this.settings.OpenKey)) ||
                    (current.IsKeyDown(Keys.Escape) && !previous.IsKeyDown(Keys.Escape)))
            {
                this.Visible = false;
                this.previousKeyboardState = current;
                base.Update(elapsed);
                return;
            }

            Viewport resolution = PhantomGame.Game.Resolution;

            bool shift = current.IsKeyDown(Keys.LeftShift) || current.IsKeyDown(Keys.RightShift);
            bool ctrl = current.IsKeyDown(Keys.LeftControl) || current.IsKeyDown(Keys.RightControl);

            // Scrollback and resize control:
            if (shift && current.IsKeyDown(Keys.Up) && !previous.IsKeyDown(Keys.Up))
                this.scrollOffset += 1;
            if (shift && current.IsKeyDown(Keys.Down) && !previous.IsKeyDown(Keys.Down))
                this.scrollOffset -= 1;
            if (current.IsKeyDown(Keys.PageUp) && !previous.IsKeyDown(Keys.PageUp))
                this.scrollOffset += this.settings.LineCount;
            if (current.IsKeyDown(Keys.PageDown) && !previous.IsKeyDown(Keys.PageDown))
                this.scrollOffset -= this.settings.LineCount;
            if (shift && current.IsKeyDown(Keys.PageUp) && !previous.IsKeyDown(Keys.PageUp))
                this.scrollOffset = int.MaxValue;
            if (shift && current.IsKeyDown(Keys.PageDown) && !previous.IsKeyDown(Keys.PageDown))
                this.scrollOffset = 0;
            if (ctrl && current.IsKeyDown(Keys.Up) && !previous.IsKeyDown(Keys.Up))
                this.settings.LineCount = Math.Max(0, this.settings.LineCount - (shift ? 5 : 1));
            if (ctrl && current.IsKeyDown(Keys.Down) && !previous.IsKeyDown(Keys.Down))
                this.settings.LineCount = Math.Min(resolution.Height / this.font.LineSpacing - 1, this.settings.LineCount + (shift ? 5 : 1));

            // Cursor control:
            int lastCursor = this.cursor;
            if (current.IsKeyDown(Keys.Left) && !previous.IsKeyDown(Keys.Left))
                this.cursor -= 1;
            if (current.IsKeyDown(Keys.Right) && !previous.IsKeyDown(Keys.Right))
                this.cursor += 1;
            if (current.IsKeyDown(Keys.Home) && !previous.IsKeyDown(Keys.End))
                this.cursor = 0;
            if (current.IsKeyDown(Keys.End) && !previous.IsKeyDown(Keys.End))
                this.cursor = this.input.Length;
            this.cursor = (int)MathHelper.Clamp(this.cursor, 0, this.input.Length);

            // Read typed keys using the KeyMap:
            Keys[] pressedKeys = current.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                Keys k = pressedKeys[i];
                if (previous.IsKeyDown(k))
                    continue;
                char c = this.keyMap.getChar(k, shift ? KeyMap.Modifier.Shift : KeyMap.Modifier.None);
                if (c != '\0')
                    this.input = this.input.Insert(this.cursor++, c.ToString());
            }
            if (current.IsKeyDown(Keys.Back) && !previous.IsKeyDown(Keys.Back) && this.cursor > 0)
            {
                this.input = this.input.Remove(this.cursor - 1, 1);
                this.cursor = (int)MathHelper.Clamp(this.cursor - 1, 0, this.input.Length);
            }
            if (current.IsKeyDown(Keys.Delete) && !previous.IsKeyDown(Keys.Delete) && this.cursor < this.input.Length)
            {
                this.input = this.input.Remove(this.cursor, 1);
                lastCursor = -1; // force reblink
            }

            if (current.IsKeyDown(Keys.Enter) && !previous.IsKeyDown(Keys.Enter))
            {
                string line = this.input.Trim();
                if (line.Length > 0)
                {
                    string[] argv = line.Split();
                    Debug.WriteLine("not executing command: `" + argv[0] + "' (not yet implemented)");
                }
                this.input = "";
                this.cursor = 0;
            }

            if (this.cursor != lastCursor)
                this.blinkTimer = 0;
            this.previousKeyboardState = current;
            base.Update(elapsed);
        }

        public override void Render(Graphics.RenderInfo info)
        {
            if (!this.Visible)
                return;
            GraphicsDevice graphicsDevice = PhantomGame.Game.GraphicsDevice;
            Viewport resolution = PhantomGame.Game.Resolution;
            float padding = this.settings.Padding;
            float lineSpace = this.font.LineSpacing;
            float height = padding * 2 + lineSpace * (this.settings.LineCount+1);
            Color color = this.settings.Color;

            this.effect.World = Matrix.Identity;
            this.effect.Projection = Matrix.CreateOrthographicOffCenter(
                0, 1, 1f/(height/resolution.Height), 0,
                0, 1);
            this.effect.DiffuseColor = this.settings.BackgroundColor.ToVector3();
            this.effect.Alpha = this.settings.Alpha;

            this.effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.SetVertexBuffer(this.backgroundBuffer);
            graphicsDevice.Indices = this.backgroundIndex;
            graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);

            this.batch.Begin();
            float y = height - padding - lineSpace;
            this.batch.DrawString(this.font, this.settings.Prompt + this.input, new Vector2(padding, y), color);
            y -= lineSpace;
            
            int count = this.lines.Count;
            this.scrollOffset = (int)MathHelper.Clamp(this.scrollOffset, 0, count - this.settings.LineCount);
            int index = 1 + this.scrollOffset;
            while ((index - this.scrollOffset) <= this.settings.LineCount && count - index >= 0)
            {
                string line = this.lines[count - index];
                IList<string> chunks = WordWrap(line, resolution.Width - padding * 2);
                for (int i = 0; i < chunks.Count; i++)
                {
                    this.batch.DrawString(this.font, chunks[i], new Vector2(padding, y), color);
                    y -= lineSpace;
                }
                index++;
            }

            this.batch.End();

            // Render cursor:
            if (this.blinkTimer % 2 < 1)
            {
                Vector2 cursorPosition = this.font.MeasureString(this.input.Substring(0, this.cursor)) + new Vector2(this.settings.Padding + this.promptWidth, 0);
                cursorPosition.Y = height - lineSpace - padding;

                this.effect.World = Matrix.CreateTranslation(cursorPosition.X, cursorPosition.Y, 0);
                this.effect.Projection = Matrix.CreateOrthographicOffCenter(
                    0, resolution.Width, resolution.Height, 0,
                    0, 1);
                this.effect.DiffuseColor = this.settings.Color.ToVector3();
                this.effect.Alpha = 1;

                this.effect.CurrentTechnique.Passes[0].Apply();
                graphicsDevice.SetVertexBuffer(this.cursorBuffer);
                graphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, 1);
            }
            base.Render(info);
        }

        private IList<string> WordWrap(string text, float widthInPixels)
        {
            this.wrapBuffer.Clear();
            float wide = this.font.MeasureString("W").X + this.font.Spacing;
            int guess = (int)(Math.Ceiling(widthInPixels / wide) + 5);

            while (text.Length > 0)
            {
                int length;
                for( length = Math.Min(guess,text.Length); this.font.MeasureString( text.Substring(0,length)).X > widthInPixels; --length );
                this.wrapBuffer.Add(text.Substring(0, length));
                text = text.Substring(length);
            }
            this.wrapBuffer.Reverse();
            return this.wrapBuffer;
        }

        public void Clear()
        {
            this.lines.Clear();
        }

        public void WriteLine(string message)
        {
            if (this.nolineBuffer.Length > 0)
            {
                message = nolineBuffer + message;
                nolineBuffer = "";
            }
            this.lines.Add(message);
        }

        public void Write(string message)
        {
            this.nolineBuffer += message;
            while (this.nolineBuffer.Contains("\n"))
            {
                string[] split = this.nolineBuffer.Split(new char[]{'\n'},1);
                this.lines.Add(split[0]);
                this.nolineBuffer = split[1];
            }
        }

        public class KeyMap
        {
            public enum Modifier : int
            {
                None,
                Shift,
            }

            private Dictionary<Keys, Dictionary<Modifier, char>> map;

            public KeyMap()
            {
                map = new Dictionary<Keys, Dictionary<Modifier, char>>();
                map[Keys.Space] = new Dictionary<Modifier, char>();
                map[Keys.Space][Modifier.None] = ' ';
                map[Keys.Space][Modifier.Shift] = ' ';

                char[] specials = { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };

                for (int i = 0; i <= 9; i++)
                {
                    char c = (char)(i + 48);
                    map[(Keys)c] = new Dictionary<Modifier, char>();
                    map[(Keys)c][Modifier.None] = c;
                    map[(Keys)c][Modifier.Shift] = specials[i];
                }

                for (char c = 'A'; c <= 'Z'; c++)
                {
                    map[(Keys)c] = new Dictionary<Modifier, char>();
                    map[(Keys)c][Modifier.None] = (char)(c + 32);
                    map[(Keys)c][Modifier.Shift] = c;
                }

                map[Keys.OemPipe] = new Dictionary<Modifier, char>();
                map[Keys.OemPipe][Modifier.None] = '\\';
                map[Keys.OemPipe][Modifier.Shift] = '|';

                map[Keys.OemOpenBrackets] = new Dictionary<Modifier, char>();
                map[Keys.OemOpenBrackets][Modifier.None] = '[';
                map[Keys.OemOpenBrackets][Modifier.Shift] = '{';

                map[Keys.OemCloseBrackets] = new Dictionary<Modifier, char>();
                map[Keys.OemCloseBrackets][Modifier.None] = ']';
                map[Keys.OemCloseBrackets][Modifier.Shift] = '}';

                map[Keys.OemComma] = new Dictionary<Modifier, char>();
                map[Keys.OemComma][Modifier.None] = ',';
                map[Keys.OemComma][Modifier.Shift] = '<';

                map[Keys.OemPeriod] = new Dictionary<Modifier, char>();
                map[Keys.OemPeriod][Modifier.None] = '.';
                map[Keys.OemPeriod][Modifier.Shift] = '>';

                map[Keys.OemSemicolon] = new Dictionary<Modifier, char>();
                map[Keys.OemSemicolon][Modifier.None] = ';';
                map[Keys.OemSemicolon][Modifier.Shift] = ':';

                map[Keys.OemQuestion] = new Dictionary<Modifier, char>();
                map[Keys.OemQuestion][Modifier.None] = '/';
                map[Keys.OemQuestion][Modifier.Shift] = '?';

                map[Keys.OemQuotes] = new Dictionary<Modifier, char>();
                map[Keys.OemQuotes][Modifier.None] = '\'';
                map[Keys.OemQuotes][Modifier.Shift] = '"';

                map[Keys.OemMinus] = new Dictionary<Modifier, char>();
                map[Keys.OemMinus][Modifier.None] = '-';
                map[Keys.OemMinus][Modifier.Shift] = '_';

                map[Keys.OemPlus] = new Dictionary<Modifier, char>();
                map[Keys.OemPlus][Modifier.None] = '=';
                map[Keys.OemPlus][Modifier.Shift] = '+';
            }

            public char getChar(Keys key, Modifier mod)
            {
                if (!map.ContainsKey(key))
                    return '\0';
                if (!map[key].ContainsKey(mod))
                    return '\0';
                return map[key][mod];
            }
        }

    }

}
