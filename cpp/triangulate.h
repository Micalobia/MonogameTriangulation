#pragma once

struct triangle
{
public:
	int a;
	int b;
	int c;
	triangle(int a, int b, int c);
	bool has(int d);
	bool has(int d, int e);
	void operator==(triangle t);
	void operator!=(triangle t);
};

