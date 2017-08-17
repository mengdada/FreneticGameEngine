﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticGameGraphics.GraphicsHelpers;
using FreneticGameCore;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace FreneticGameGraphics.ClientSystem
{
    /// <summary>
    /// Represents a Three-Dimensional game engine.
    /// </summary>
    public class GameEngine3D : GameEngineBase
    {
        /// <summary>
        /// Sets up the game engine 3D.
        /// Considering also attaching to available events such as <see cref="GameEngineBase.OnWindowSetUp"/>.
        /// Then call <see cref="GameEngineBase.Start"/>.
        /// </summary>
        /// <param name="_windowTitle">The title, if different from game program descriptor.</param>
        public GameEngine3D(string _sWindowTitle) : base(_sWindowTitle)
        {
        }

        /// <summary>
        /// The list of common shaders for this engine.
        /// </summary>
        public GE3DShaders Shaders3D = new GE3DShaders();

        /// <summary>
        /// Whether to allow LL light helpers.
        /// </summary>
        public bool AllowLL = false;

        /// <summary>
        /// Whether to enable forward lights.
        /// </summary>
        public bool Forward_Lights = true;

        /// <summary>
        /// Whether to enable forward normal effects.
        /// </summary>
        public bool Forward_Normals = true;

        /// <summary>
        /// Whether to enable forward shadows.
        /// </summary>
        public bool Forward_Shadows = false;

        /// <summary>
        /// Loads all shaders for the standard Game Engine 3D.
        /// </summary>
        public override void GetShaders()
        {
            Shaders3D.LoadAll(Shaders, AllowLL, Forward_Normals, Forward_Lights, Forward_Shadows);
        }

        /// <summary>
        /// The render helper system.
        /// </summary>
        public Renderer Rendering;

        /// <summary>
        /// System to help with models.
        /// </summary>
        public ModelEngine Models;

        /// <summary>
        /// Whether forward mode should calculate reflection helpers.
        /// </summary>
        public bool ForwardReflections = false;

        /// <summary>
        /// Whether to display decal effects.
        /// </summary>
        public bool DisplayDecals = true;

        /// <summary>
        /// Whether to render the view as a 3D side-by-side view.
        /// </summary>
        public bool Render3DView = false;

        /// <summary>
        /// The current Field Of View, in degrees (Defaults to 70).
        /// </summary>
        public float FOV = 70;

        /// <summary>
        /// The current Z-Near value, defaults to '0.1'.
        /// </summary>
        public float ZNear = 0.1f;

        /// <summary>
        /// Get the Z-Far value (defaults to 1000 autoget).
        /// </summary>
        public Func<float> ZFar = () => 1000;
        
        /// <summary>
        /// Get the maximum distance of fog. Defaults to match ZFar.
        /// </summary>
        public Func<float> FogMaxDist = null;

        /// <summary>
        /// Get the Z-Far (OUT-View) value (defaults to 1000 autoget).
        /// </summary>
        public Func<float> ZFarOut = () => 1000;

        /// <summary>
        /// The "Sun adjustment" backup light color and value.
        /// </summary>
        public Vector4 SunAdjustBackupLight = Vector4.One;

        /// <summary>
        /// The direction of the sun for backup light.
        /// </summary>
        public Location SunAdjustDirection = -Location.UnitZ;

        /// <summary>
        /// Whether dynamic shadows should be handled at all.
        /// </summary>
        public bool EnableDynamicShadows = true;

        /// <summary>
        /// The main 3D view.
        /// </summary>
        public View3D MainView = null;

        /// <summary>
        /// Loads any additional final data.
        /// </summary>
        public override void PostLoad()
        {
            FogMaxDist = () => ZFar();
            GraphicsUtil.CheckError("PostLoad - Pre");
            SysConsole.Output(OutputType.INIT, "GameEngine configuring graphics...");
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);
            SysConsole.Output(OutputType.INIT, "GameEngine loading model engine...");
            Models = new ModelEngine();
            Models.Init(null, this);
            SysConsole.Output(OutputType.INIT, "GameEngine loading render helper...");
            Rendering = new Renderer(Textures, Shaders, Models);
            Rendering.Init();
            SysConsole.Output(OutputType.INIT, "GameEngine loading main 3D view...");
            MainView = new View3D();
            MainView.Generate(this, Window.Width, Window.Height);
            MainView.Render3D = Render3D;
            MainView.PostFirstRender = ReverseEntities;
            GraphicsUtil.CheckError("PostLoad - Post");
        }

        /// <summary>
        /// Sorts the entities according to distance from camera view.
        /// </summary>
        public void SortEntities()
        {
            Location pos = MainView.CameraPos;
            Entities = Entities.OrderBy((e) => e.LastKnownPosition.DistanceSquared(pos)).ToList();
        }

        /// <summary>
        /// Reverses the entity order for transparent rendering.
        /// </summary>
        public void ReverseEntities()
        {
            Entities.Reverse();
        }

        /// <summary>
        /// Renders the standard view's 3D data.
        /// </summary>
        /// <param name="view">The view object.</param>
        public void Render3D(View3D view)
        {

        }

        /// <summary>
        /// Renders a single frame of the 3D game engine.
        /// </summary>
        public override void RenderSingleFrame()
        {
            Models.Update(GlobalTickTime);
            SortEntities();
            MainView.Render();
            ReverseEntities();
        }
    }
}
