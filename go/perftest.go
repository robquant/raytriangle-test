package main

import (
	"flag"
	"fmt"
	"math"
	"time"

	"golang.org/x/exp/rand"
)

type Vec3 struct {
	x, y, z float32
}

type Ray struct {
	Orig, Direction Vec3
}

func (self Vec3) Sub(v Vec3) Vec3 {
	return Vec3{self.x - v.x, self.y - v.y, self.z - v.z}
}

func (self Vec3) Dot(v Vec3) float32 {
	return self.x*v.x + self.y*v.y + self.z*v.z
}

func (self Vec3) Cross(v Vec3) Vec3 {
	return Vec3{
		self.y*v.z - self.z*v.y,
		self.z*v.x - self.x*v.z,
		self.x*v.y - self.y*v.x}
}

func (self Vec3) Length() float32 {
	return float32(math.Sqrt(float64(self.x*self.x + self.y*self.y + self.z*self.z)))
}

func (self Vec3) Normalize() Vec3 {
	len := self.Length()
	return Vec3{self.x / len, self.y / len, self.z / len}
}

func ray_triangle_intersect(r *Ray, v0, v1, v2 *Vec3) float32 {
	v0v1 := v1.Sub(*v0)
	v0v2 := v2.Sub(*v0)
	pvec := r.Direction.Cross(v0v2)

	det := v0v1.Dot(pvec)

	if det < 0.000001 {
		return float32(math.Inf(-1))
	}

	invDet := 1.0 / det
	tvec := r.Orig.Sub(*v0)
	u := tvec.Dot(pvec) * invDet

	if u < 0 || u > 1 {
		return float32(math.Inf(-1))
	}

	qvec := tvec.Cross(v0v1)
	v := r.Direction.Dot(qvec) * invDet

	if v < 0 || u+v > 1 {
		return float32(math.Inf(-1))
	}

	return v0v2.Dot(qvec) * invDet
}

func random_vertex() Vec3 {
	return Vec3{rand.Float32()*2.0 - 1.0,
		rand.Float32()*2.0 - 1.0,
		rand.Float32()*2.0 - 1.0}
}

func generate_random_triangles(numTriangles int) []Vec3 {
	vertices := make([]Vec3, numTriangles*3)

	for i := 0; i < numTriangles; i++ {
		vertices[i*3+0] = random_vertex()
		vertices[i*3+1] = random_vertex()
		vertices[i*3+2] = random_vertex()
	}
	return vertices
}

func random_sphere(rgen *rand.Rand) Vec3 {
	r1 := rgen.Float64()
	r2 := rgen.Float64()
	lat := math.Acos(2*r1-1) - math.Pi/2
	lon := 2 * math.Pi * r2

	return Vec3{float32(math.Cos(lat) * math.Cos(lon)),
		float32(math.Cos(lat) * math.Sin(lon)),
		float32(math.Sin(lat))}
}

const NUM_RAYS = 400
const NUM_TRIANGLES = 1000 * 1000

type Result struct {
	numHit, numMiss int
}

func main() {
	vertices := generate_random_triangles(NUM_TRIANGLES)
	num_vertices := NUM_TRIANGLES * 3

	nParallelPtr := flag.Int("nPar", 1, "number of parallel processes")
	flag.Parse()

	resultChan := make(chan Result)
	nParallel := *nParallelPtr

	numHit := 0
	numMiss := 0

	t_start := time.Now()

	for i := 0; i < nParallel; i++ {
		go func(result chan Result) {

			seed := time.Now().UnixNano()
			rgen := rand.New(rand.NewSource(uint64(seed)))
			numHit := 0
			numMiss := 0
			r := Ray{}
			for i := 0; i < NUM_RAYS; i++ {
				r.Orig = random_sphere(rgen)
				r.Direction = random_sphere(rgen).Sub(r.Orig).Normalize()

				for j := 0; j < num_vertices/3; j++ {
					t := ray_triangle_intersect(&r, &vertices[j*3+0],
						&vertices[j*3+1],
						&vertices[j*3+2])
					if t >= 0 {
						numHit += 1
					} else {
						numMiss += 1
					}
				}
			}
			resultChan <- Result{numHit, numMiss}
		}(resultChan)
	}

	for i := 0; i < nParallel; i++ {
		result := <-resultChan
		numHit += result.numHit
		numMiss += result.numMiss
	}

	t_total := time.Since(t_start)

	num_tests := NUM_RAYS * NUM_TRIANGLES * nParallel
	// hit_perc := float64(num_hit) / float64(num_tests) * 100
	// miss_perc := float64(num_miss) / float64(num_tests) * 100
	mtests_per_second := float64(num_tests) / t_total.Seconds() / 1000000
	fmt.Printf("Hits: %v\n", numHit)
	fmt.Printf("Millions of tests per second: %.2f\n", mtests_per_second)
}
