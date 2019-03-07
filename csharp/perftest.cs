using System;
using System.Runtime.CompilerServices;


namespace perftest
{

    struct Vector3
    {
        public float x, y, z;

        // public static implicit operator Vector3(float v) { return new Vector3(a: v, b: v, c: v); }

        public Vector3(float a, float b, float c = 0)
        {
            x = a;
            y = b;
            z = c;
        }

        public static Vector3 operator -(Vector3 q, Vector3 r) { return new Vector3(q.x - r.x, q.y - r.y, q.z - r.z); }

        public static Vector3 operator +(Vector3 q, Vector3 r) { return new Vector3(q.x + r.x, q.y + r.y, q.z + r.z); }

        public static Vector3 operator *(Vector3 q, float r) { return new Vector3(q.x * r, q.y * r, q.z * r); }

        public static Vector3 Cross(Vector3 q, Vector3 r)
        {
            return new Vector3(q.y * r.z - q.z * r.y,
             q.z * r.x - q.x * r.z,
             q.x * r.y - q.y * r.x);
        }

        public static float operator %(Vector3 q, Vector3 r) { return q.x * r.x + q.y * r.y + q.z * r.z; }

        public static Vector3 operator !(Vector3 q)
        {
            return q * (1.0f / MathF.Sqrt(q % q));
        }
    }

    struct Ray
    {
        public Ray(Vector3 orig_, Vector3 dir_)
        {
            orig = orig_;
            dir = dir_;
        }

        public Vector3 orig, dir;
    }

    class Program
    {
        private static Random rnd = new Random();

        private static float rayTriangleIntersect(ref Ray r, ref Vector3 v0, ref Vector3 v1, ref Vector3 v2)
        {
            Vector3 v0v1 = v1 - v0;
            Vector3 v0v2 = v2 - v0;

            Vector3 pvec = Vector3.Cross(r.dir, v0v2);

            float det = v0v1 % pvec;

            if (det < 0.000001)
                return float.NegativeInfinity;

            float invDet = 1.0f / det;

            Vector3 tvec = r.orig - v0;

            float u = (tvec % pvec) * invDet;

            if (u < 0 || u > 1)
                return float.NegativeInfinity;

            Vector3 qvec = Vector3.Cross(tvec, v0v1);

            float v = (r.dir % qvec) * invDet;

            if (v < 0 || u + v > 1)
                return float.NegativeInfinity;

            return (v0v2 % qvec) * invDet;
        }

        private static Vector3 randomSphere()
        {
            float r1 = (float)rnd.NextDouble();
            float r2 = (float)rnd.NextDouble();
            float lat = MathF.Acos(2 * r1 - 1) - MathF.PI / 2;
            float lon = 2 * MathF.PI * r2;

            return new Vector3(
              MathF.Cos(lat) * MathF.Cos(lon),
              MathF.Cos(lat) * MathF.Sin(lon),
              MathF.Sin(lat)
            );
        }

        private static Vector3[] generateRandomTriangles(int numTriangles)
        {
            Vector3[] vertices = new Vector3[numTriangles * 3];

            for (int i = 0; i < numTriangles; ++i)
            {
                vertices[i * 3 + 0] = randomSphere();
                vertices[i * 3 + 1] = randomSphere();
                vertices[i * 3 + 2] = randomSphere();
            }

            return vertices;
        }

        static void Main(string[] args)
        {
            const int NUM_RAYS = 1000;
            const int NUM_TRIANGLES = 100 * 1000;

            Vector3[] vertices = generateRandomTriangles(NUM_TRIANGLES);
            const int numVertices = NUM_TRIANGLES * 3;

            int numHit = 0;
            int numMiss = 0;

            Ray r;

            var start = DateTime.UtcNow;

            for (int i = 0; i < NUM_RAYS; ++i)
            {
                r.orig = randomSphere();
                Vector3 p1 = randomSphere();
                r.dir = !(p1 - r.orig);

                for (int j = 0; j < numVertices / 3; ++j)
                {
                    float t = rayTriangleIntersect(ref r,
                                                   ref vertices[j * 3 + 0],
                                                   ref vertices[j * 3 + 1],
                                                   ref vertices[j * 3 + 2]);
                    if (t >= 0)
                    {
                        ++numHit;
                    }
                    else
                    {
                        ++numMiss;
                    }
                }
            }

            float tTotal = (float)((DateTime.UtcNow - start).TotalSeconds);

            int numTests = NUM_RAYS * NUM_TRIANGLES;
            float hitPerc = ((float)numHit / numTests) * 100.0f;
            float missPerc = ((float)numMiss / numTests) * 100.0f;
            float mTestsPerSecond = (float)numTests / tTotal / 1000000.0f;

            Console.WriteLine(String.Format("Total intersection tests:  {0}", numTests));
            Console.WriteLine(String.Format("  Hits:\t\t\t    {0} ({1}%)", numHit, hitPerc));
            Console.WriteLine(String.Format("  Misses:\t\t    {0} ({1}%)", numMiss, missPerc));
            Console.WriteLine(String.Format("Total time:\t\t\t{0} seconds", tTotal));
            Console.WriteLine(String.Format("Millions of tests per second:\t{0}", mTestsPerSecond));
        }
    }
}
