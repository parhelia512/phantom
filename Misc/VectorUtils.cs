﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Phantom.Misc
{
    public static class VectorUtils
    {
        /// <summary>
        /// Normalize a vector only if the length isn't zero. This makes sure a division by zero doesn't occure.
        /// 
        /// FIXME: This doens't seem to work...
        /// </summary>
        /// <param name="v">The vector to normalize.</param>
        public static Vector2 SafeNormalize(this Vector2 v)
        {
            if (v.LengthSquared() > 0)
                v.Normalize();
            return v;
        }

        public static Vector3 Normalized(this Vector3 self)
        {
            if (self.LengthSquared() == 0)
                return self;
            Vector3 result = self;
            result.Normalize();
            return result;
        }

        public static Vector2 Normalized(this Vector2 self)
        {
            if (self.LengthSquared() == 0)
                return self;
            Vector2 result = self;
            result.Normalize();
            return result;
        }

        public static Vector2 LeftPerproduct(this Vector2 self)
        {
            return new Vector2(self.Y, -self.X);
        }
        public static Vector2 RightPerproduct(this Vector2 self)
        {
            return new Vector2(-self.Y, self.X);
        }

        public static float Angle(this Vector2 v)
        {
            return (float)Math.Atan2(v.Y, v.X);
        }

        public static Vector2 RotateBy(this Vector2 v, float angle)
        {
            Vector2 r = new Vector2();
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            r.X = cos * v.X - sin * v.Y;
            r.Y = sin * v.X + cos * v.Y;
            return r;
        }

        public static Vector2 Flatten(this Vector3 self)
        {
            return new Vector2(self.X, self.Y);
        }

        public static Vector3 GetRandom()
        {
            return new Vector3((float)PhantomGame.Randy.NextDouble(), (float)PhantomGame.Randy.NextDouble(), (float)PhantomGame.Randy.NextDouble());
        }
    }
}
