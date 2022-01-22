// Integrate.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <chrono>
using namespace std::chrono;

double f(double x) {
	return 4.0 / (1.0 + x * x);
}

double integrate(double min, double max, int steps) {
	double stepSize = (max - min) / steps;
	double result = 0.0;
	for (int i = 0; i < steps; i++) {
		result += f(min + stepSize * i) * stepSize;
	}
	return result;
}

double integrateOMP(double min, double max, int steps) {
	double stepSize = (max - min) / steps;
	double result = 0.0;

#pragma omp parallel for reduction(+:result)
	for (int i = 0; i < steps; i++) {
		result += f(min + stepSize * i) * stepSize;
	}
	return result;
}

int main() {
	// Threadpool has to be created thats why omp variant is initially slower
	auto resOMP = integrateOMP(0.0, 1.0, 0);
	std::cout << resOMP;

	int n;
	std::cout << std::endl << "Enter n: ";
	std::cin >> n;

	std::cout << "StepSize " << "ResultSeq " << "TimeSeq " << "ResultPar " << "TimePar" << std::endl;

	while (n < 100000000) {
		auto start = std::chrono::high_resolution_clock::now();
		auto res = integrate(0.0, 1.0, n);
		std::chrono::duration<double, std::milli> time = std::chrono::high_resolution_clock::now() - start;
		double timeSeq = (long long)(duration_cast<microseconds>(time).count());

		start = std::chrono::high_resolution_clock::now();
		auto resOMP = integrateOMP(0.0, 1.0, n);
		time = std::chrono::high_resolution_clock::now() - start;
		double timeOMP = (long long)(duration_cast<microseconds>(time).count());

		std::cout << n << " " << res << " " << timeSeq << " " << resOMP << " " << timeOMP << std::endl;
		n *= 10;
	}
}