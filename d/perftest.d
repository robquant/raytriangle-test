import std.stdio;
import std.math;
import std.datetime;
import std.random;
import std.datetime.stopwatch: StopWatch, AutoStart;

struct Vec3 {
    float x,y,z;

    this(float x, float y, float z) {
        this.x = x;
        this.y = y; 
        this.z = z;
    }
}

struct Ray
{
  Vec3 orig;
  Vec3 dir;
};

Vec3 sub(Vec3 a, Vec3 b)
{
  return Vec3(a.x - b.x, a.y - b.y, a.z - b.z);
}

float dot(Vec3 a, Vec3 b)
{
  return a.x * b.x + a.y * b.y + a.z * b.z;
}

float len(Vec3 v)
{
  return sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
}

Vec3 normalize(Vec3 v)
{
  float l = len(v);
  return Vec3(v.x / l, v.y / l, v.z / l );
}

Vec3 cross(Vec3 a, Vec3 b)
{
  return Vec3(
    a.y * b.z - a.z * b.y,
    a.z * b.x - a.x * b.z,
    a.x * b.y - a.y * b.x
  );
}

float rayTriangleIntersect(Ray *r, Vec3 *v0, Vec3 *v1, Vec3 *v2)
{
  Vec3 v0v1 = sub(*v1, *v0);
  Vec3 v0v2 = sub(*v2, *v0);

  Vec3 pvec = cross(r.dir, v0v2);

  float det = dot(v0v1, pvec);

  if (det < 0.000001)
    return -real.infinity;

  float invDet = 1.0 / det;

  Vec3 tvec = sub(r.orig, *v0);

  float u = dot(tvec, pvec) * invDet;

  if (u < 0 || u > 1)
    return -real.infinity;

  Vec3 qvec = cross(tvec, v0v1);

  float v = dot(r.dir, qvec) * invDet;

  if (v < 0 || u + v > 1)
    return -real.infinity;

  return dot(v0v2, qvec) * invDet;
}

Vec3 randomSphere()
{
  double r1 = uniform01();
  double r2 = uniform01();
  double lat = acos(2*r1 - 1) - PI/2;
  double lon = 2 * PI * r2;

  return Vec3(
    cast(float)(cos(lat) * cos(lon)),
    cast(float)(cos(lat) * sin(lon)),
    cast(float)(sin(lat))
  );
}

Vec3[] generateRandomTriangles(int numTriangles)
{

  Vec3[] vertices = new Vec3[numTriangles * 3];

  for (int i = 0; i < numTriangles; ++i)
  {
    vertices[i*3 + 0] = randomSphere();
    vertices[i*3 + 1] = randomSphere();
    vertices[i*3 + 2] = randomSphere();
  }

  return vertices;
}


int main()
{
  const int NUM_RAYS = 1000;
  const int NUM_TRIANGLES = 100 * 1000;

  Vec3[] vertices = generateRandomTriangles(NUM_TRIANGLES);
  const int numVertices = NUM_TRIANGLES * 3;

  int numHit = 0;
  int numMiss = 0;

  Ray r;

  auto sw = StopWatch(AutoStart.no);
  sw.start();

  for (int i = 0; i < NUM_RAYS; ++i)
  {
    r.orig = randomSphere();
    Vec3 p1 = randomSphere();
    r.dir  = normalize((sub(p1, r.orig)));

    for (int j = 0; j < numVertices / 3; ++j)
    {
      float t = rayTriangleIntersect(&r,
                                     &vertices[j*3 + 0],
                                     &vertices[j*3 + 1],
                                     &vertices[j*3 + 2]);
      t >= 0 ? ++numHit : ++numMiss;
    }
  }

  sw.stop();
  Duration dt = sw.peek();

  int numTests = NUM_RAYS * NUM_TRIANGLES;
  float hitPerc  = (cast(float)(numHit)  / numTests) * 100.0f;
  float missPerc = (cast(float)(numMiss) / numTests) * 100.0f;
  float mTestsPerSecond = cast(float)numTests / (cast(float)(dt.total!"msecs")/1000.0) / 1000000.0f;

  printf("Total intersection tests:  %'10i\n", numTests);
  printf("  Hits:\t\t\t    %'10i (%5.2f%%)\n", numHit, hitPerc);
  printf("  Misses:\t\t    %'10i (%5.2f%%)\n", numMiss, missPerc);
  printf("\n");
  printf("Total time:\t\t\t%6.2f seconds\n", cast(double)dt.total!"msecs"/1000.0);
  printf("Millions of tests per second:\t%6.2f\n", mTestsPerSecond);
  return 0;
}
