// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

// ReSharper disable AccessToDisposedClosure
using System.Collections.Generic;
using DrawWithSilkyNvg;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SilkyNvg;
using SilkyNvg.Rendering.OpenGL;
using SixLabors.Fonts;

OpenGLRenderer renderer = null!;

SilkyNvgGlyphRenderer glypher = null!;

ImGuiController imgui = null!;

IInputContext input = null!;

GL gl = null!;

const string title = "Vector Text Demo using Silk.NET, SilkyNvg, and SixLabors.Fonts!";

// Create a window for OpenGL 4.5.
IWindow window = Window.Create(WindowOptions.Default with
{
    API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(4, 5)),
    Title = title,
    PreferredBitDepth = Vector4D<int>.One * 8,
    PreferredDepthBufferBits = 24,
    PreferredStencilBufferBits = 8
});

// Set everything up when the window loads up
window.Load += () =>
{
    // Create the OpenGL API object for our window's context.
    gl = window.CreateOpenGL();

    // Create the input context for receiving keyboard and mouse input.
    input = window.CreateInput();

    // Create the ImGui controller
    imgui = new ImGuiController(gl, window, input);

    // Create the SilkyNvg renderer
    renderer = new OpenGLRenderer(CreateFlags.Antialias | CreateFlags.Debug, gl);
    glypher = new SilkyNvgGlyphRenderer { Nvg = Nvg.Create(renderer) };
    gl.Viewport(window.FramebufferSize);
};

var fonts = new List<Font>();

var demoWindow = false;

window.Update += deltaSecs =>
{
    imgui.Update((float)deltaSecs);
    if (ImGui.Begin("Interactive Demo"))
    {
        var checkVal = glypher.DrawGlyphBox;
        ImGui.Checkbox("Show Glyph Bounding Boxes", ref checkVal);
        glypher.DrawGlyphBox = checkVal;
        checkVal = glypher.DrawTextBox;
        ImGui.Checkbox("Show Text Bounding Boxes", ref checkVal);
        glypher.DrawTextBox = checkVal;
        ImGui.Checkbox("Show ImGui Demo Window", ref demoWindow);
        if (demoWindow)
        {
            ImGui.ShowDemoWindow();
        }

        // TODO add/edit/remove fonts in use by demo
        // TODO add/edit/remove test text
        ImGui.End();
    }
};

window.Render += deltaSecs =>
{
    window.Title = $"{title} | {1 / deltaSecs} FPS";

    gl.ClearColor(Vector4D<float>.Zero);
    gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    glypher.Nvg.BeginFrame(window.FramebufferSize.As<float>(), 1);

    Font font = SystemFonts.CreateFont("arial", 40f);
    var tr = new TextRenderer(glypher);
    tr.RenderText("The quick brown fox jumps over the lazy dog.", new TextOptions(font));

    glypher.Nvg.EndFrame();
    gl.Viewport(window.FramebufferSize);
    imgui.Render();
};

window.Closing += () =>
{
    renderer.Dispose();
    gl.Dispose();
    input.Dispose();
};

window.FramebufferResize += newSize =>
{
    gl.Viewport(newSize);
};

window.Run();

window.Dispose();
