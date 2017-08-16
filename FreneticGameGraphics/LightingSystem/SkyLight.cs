﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticGameCore;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace FreneticGameGraphics.LightingSystem
{
    /// <summary>
    /// Represents a light from the sky.
    /// </summary>
    public class SkyLight : LightObject
    {
        /// <summary>
        /// The radius of the light.
        /// </summary>
        float Radius;

        /// <summary>
        /// The color of the light.
        /// </summary>
        public Location Color;

        /// <summary>
        /// The direction of the light.
        /// </summary>
        public Location Direction;

        /// <summary>
        /// The width of effect of the light.
        /// </summary>
        public float Width;

        /// <summary>
        /// The FrameBufferObject.
        /// </summary>
        public int FBO = -1;

        /// <summary>
        /// The FBO Texture.
        /// </summary>
        public int FBO_Tex = -1;

        /// <summary>
        /// The FBO Depth Texture.
        /// </summary>
        public int FBO_DepthTex = -1;

        /// <summary>
        /// The width of the shadow texture.
        /// </summary>
        public int TexWidth = 0;

        /// <summary>
        /// Constructs the sky light.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="col">The color.</param>
        /// <param name="dir">The direction.</param>
        /// <param name="size">Effective size.</param>
        /// <param name="transp">Whether to include transparents for shadow effects.</param>
        /// <param name="twidth">The shadow texture width.</param>
        public SkyLight(Location pos, float radius, Location col, Location dir, float size, bool transp, int twidth)
        {
            EyePos = pos;
            Radius = radius;
            Color = col;
            Width = size;
            InternalLights.Add(new LightOrtho());
            if (dir.Z >= 0.99 || dir.Z <= -0.99)
            {
                InternalLights[0].up = new Vector3(0, 1, 0);
            }
            else
            {
                InternalLights[0].up = new Vector3(0, 0, 1);
            }
            InternalLights[0].transp = transp;
            Direction = dir;
            InternalLights[0].Create(pos.ToOpenTK3D(), (pos + dir).ToOpenTK3D(), Width, Radius, Color.ToOpenTK());
            MaxDistance = radius;
            TexWidth = twidth;
            FBO = GL.GenFramebuffer();
            FBO_Tex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FBO_Tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, TexWidth, TexWidth, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            FBO_DepthTex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, FBO_DepthTex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, TexWidth, TexWidth, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, FBO_Tex, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, FBO_DepthTex, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        /// <summary>
        /// Destroys the sky light.
        /// </summary>
        public void Destroy()
        {
            InternalLights[0].Destroy();
            GL.DeleteFramebuffer(FBO);
            GL.DeleteTexture(FBO_Tex);
            GL.DeleteTexture(FBO_DepthTex);
        }

        /// <summary>
        /// Repositions the sky light.
        /// </summary>
        /// <param name="pos">New position.</param>
        public override void Reposition(Location pos)
        {
            EyePos = pos;
            InternalLights[0].NeedsUpdate = true;
            InternalLights[0].eye = EyePos.ToOpenTK3D();
            InternalLights[0].target = (EyePos + Direction).ToOpenTK3D();
        }
    }
}