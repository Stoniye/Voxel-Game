using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Voxel_Game.res.scripts
{
    public class Chunk
    {
        public const int ChunkSize = 16;
        private readonly byte[,,] _blocks;
        private int _vertexBufferObject;
        private int _vertexArrayObject;
        private int _elementBufferObject;
        private int _vertexCount;
        private readonly Vector3 _position;
        private readonly Shader _shader;

        public Chunk(Vector3 position, Shader shader)
        {
            _position = position;
            _shader = shader;
            _blocks = new byte[ChunkSize, ChunkSize, ChunkSize];
            InitializeBlocks();
            GenerateMesh();
            SetupBuffers();
        }

        private void InitializeBlocks()
        {
            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        _blocks[x, y, z] = (y <= 8) ? (byte)1 : (byte)0;
                    }
                }
            }
        }

        private void GenerateMesh()
        {
            List<float> vertices = new List<float>();
            List<uint> indices = new List<uint>();
            uint index = 0;

            for (int x = 0; x < ChunkSize; x++)
            {
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int z = 0; z < ChunkSize; z++)
                    {
                        if (_blocks[x, y, z] == 0) continue;

                        Vector3 blockPos = new Vector3(x, y, z);

                        // Front face (z + 1)
                        if (IsTransparent(x, y, z + 1))
                        {
                            AddFace(vertices, indices, blockPos, new Vector3(0, 0, 1), ref index);
                        }

                        // Back face (z - 1)
                        if (IsTransparent(x, y, z - 1))
                        {
                            AddFace(vertices, indices, blockPos, new Vector3(0, 0, -1), ref index);
                        }

                        // Top face (y + 1)
                        if (IsTransparent(x, y + 1, z))
                        {
                            AddFace(vertices, indices, blockPos, new Vector3(0, 1, 0), ref index);
                        }

                        // Bottom face (y - 1)
                        if (IsTransparent(x, y - 1, z))
                        {
                            AddFace(vertices, indices, blockPos, new Vector3(0, -1, 0), ref index);
                        }

                        // Left face (x - 1)
                        if (IsTransparent(x - 1, y, z))
                        {
                            AddFace(vertices, indices, blockPos, new Vector3(-1, 0, 0), ref index);
                        }

                        // Right face (x + 1)
                        if (IsTransparent(x + 1, y, z))
                        {
                            AddFace(vertices, indices, blockPos, new Vector3(1, 0, 0), ref index);
                        }
                    }
                }
            }

            _vertices = vertices.ToArray();
            _indices = indices.ToArray();
            _vertexCount = indices.Count;
        }

        private bool IsTransparent(int x, int y, int z)
        {
            if (x < 0 || x >= ChunkSize || y < 0 || y >= ChunkSize || z < 0 || z >= ChunkSize)
                return true;
            return _blocks[x, y, z] == 0;
        }

        private void AddFace(List<float> vertices, List<uint> indices, Vector3 pos, Vector3 normal, ref uint index)
        {
            Vector3[] faceVertices = new Vector3[4];
            if (normal == new Vector3(0, 0, 1)) // Front
            {
                faceVertices[0] = pos + new Vector3(-0.5f, -0.5f, 0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, -0.5f, 0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, 0.5f, 0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, 0.5f, 0.5f);
            }
            else if (normal == new Vector3(0, 0, -1)) // Back
            {
                faceVertices[0] = pos + new Vector3(0.5f, -0.5f, -0.5f);
                faceVertices[1] = pos + new Vector3(-0.5f, -0.5f, -0.5f);
                faceVertices[2] = pos + new Vector3(-0.5f, 0.5f, -0.5f);
                faceVertices[3] = pos + new Vector3(0.5f, 0.5f, -0.5f);
            }
            else if (normal == new Vector3(0, 1, 0)) // Top
            {
                faceVertices[0] = pos + new Vector3(-0.5f, 0.5f, -0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, 0.5f, -0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, 0.5f, 0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, 0.5f, 0.5f);
            }
            else if (normal == new Vector3(0, -1, 0)) // Bottom
            {
                faceVertices[0] = pos + new Vector3(-0.5f, -0.5f, 0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, -0.5f, 0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, -0.5f, -0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, -0.5f, -0.5f);
            }
            else if (normal == new Vector3(-1, 0, 0)) // Left
            {
                faceVertices[0] = pos + new Vector3(-0.5f, -0.5f, -0.5f);
                faceVertices[1] = pos + new Vector3(-0.5f, -0.5f, 0.5f);
                faceVertices[2] = pos + new Vector3(-0.5f, 0.5f, 0.5f);
                faceVertices[3] = pos + new Vector3(-0.5f, 0.5f, -0.5f);
            }
            else if (normal == new Vector3(1, 0, 0)) // Right
            {
                faceVertices[0] = pos + new Vector3(0.5f, -0.5f, 0.5f);
                faceVertices[1] = pos + new Vector3(0.5f, -0.5f, -0.5f);
                faceVertices[2] = pos + new Vector3(0.5f, 0.5f, -0.5f);
                faceVertices[3] = pos + new Vector3(0.5f, 0.5f, 0.5f);
            }

            for (int i = 0; i < 4; i++)
            {
                vertices.Add(faceVertices[i].X);
                vertices.Add(faceVertices[i].Y);
                vertices.Add(faceVertices[i].Z);
                vertices.Add(1.0f); // Red
                vertices.Add(0.0f); // Green
                vertices.Add(0.0f); // Blue
            }

            indices.Add(index + 0);
            indices.Add(index + 1);
            indices.Add(index + 2);
            indices.Add(index + 2);
            indices.Add(index + 3);
            indices.Add(index + 0);
            index += 4;
        }

        private void SetupBuffers()
        {
            _vertexArrayObject = GL.GenVertexArray();
            _vertexBufferObject = GL.GenBuffer();
            _elementBufferObject = GL.GenBuffer();

            GL.BindVertexArray(_vertexArrayObject);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);
            GL.BufferSubData(BufferTarget.ArrayBuffer, 0, _vertices.Length * sizeof(float), _vertices);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices,
                BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            _shader.Use();
            Matrix4 model = Matrix4.CreateTranslation(_position);
            _shader.SetMatrix4("model", model);
            _shader.SetMatrix4("view", view);
            _shader.SetMatrix4("projection", projection);

            GL.BindVertexArray(_vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_elementBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
        }

        private float[] _vertices;
        private uint[] _indices;
    }
}