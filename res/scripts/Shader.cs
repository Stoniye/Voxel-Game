using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Voxel_Game.res.scripts
{
    public class Shader
    {
        private readonly int _program;
        private bool _disposedValue;

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);
            
            int vertexShader;
            int fragmentShader;
            
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);

            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            
            GL.CompileShader(vertexShader);

            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(vertexShader);
                Console.WriteLine(infoLog);
            }
            
            GL.CompileShader(fragmentShader);

            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(fragmentShader);
                Console.WriteLine(infoLog);
            }
            
            _program = GL.CreateProgram();

            GL.AttachShader(_program, vertexShader);
            GL.AttachShader(_program, fragmentShader);

            GL.LinkProgram(_program);

            GL.GetProgram(_program, GetProgramParameterName.LinkStatus, out success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(_program);
                Console.WriteLine(infoLog);
            }
            
            GL.DetachShader(_program, vertexShader);
            GL.DetachShader(_program, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);
        }
        
        public void Use()
        {
            GL.UseProgram(_program);
        }
        
        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                GL.DeleteProgram(_program);
                _disposedValue = true;
            }
        }

        ~Shader()
        {
            if (_disposedValue == false)
            {
                Console.WriteLine("GPU Resource leak! Did you forget to call Dispose()?");
            }
        }
        
        public void SetMatrix4(string name, Matrix4 matrix)
        {
            int location = GL.GetUniformLocation(_program, name);
            GL.UniformMatrix4(location, false, ref matrix);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}