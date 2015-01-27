﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phantom.GameUI
{
    public class ToolTip : UIElement
    {
        public static Type ToolTipType = typeof(ToolTip);
        public static float FontScale = 1f;
        public static float Offset = 40f;
        protected string label;
        protected Vector2 size;
        public ToolTip()
            : base("ToolTip", new Vector2(0,0), null)
        {}

        public virtual void SetText(string label)
        {
            this.label = label;
            if (UILayer.Font != null)
                size = UILayer.Font.MeasureString(label);
            else 
                size = new Vector2(0,0);
        }

        public void SetPosition(Vector2 position)
        {
            this.Position = position;
            if (this.Position.Y > Offset + size.Y * 0.5f * FontScale)
                this.Position += new Vector2(0, -Offset);
            else
                this.Position += new Vector2(0, Offset);
            if (this.Position.X + size.X * 0.5f * FontScale > PhantomGame.Game.Resolution.Width)
                this.Position.X = PhantomGame.Game.Resolution.Width - size.X * 0.5f * FontScale;
            if (this.Position.X - size.X * 0.5f * FontScale < 0)
                this.Position.X = size.X * 0.5f * FontScale;
        }

        public override void Render(Graphics.RenderInfo info)
        {
            base.Render(info);
            if (UILayer.Font != null && info.Pass == info.Renderer.Passes - 1)
            {
                info.Canvas.FillColor = Color.Black;
                info.Canvas.FillRect(this.Position, size * 0.5f, 0);
                info.Batch.DrawString(UILayer.Font, label, this.Position, Color.White, 0, size * 0.5f, 1, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0);
            }
        }
    }
}
