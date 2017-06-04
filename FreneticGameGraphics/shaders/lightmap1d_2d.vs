#version 430 core

layout (location = 0) in vec3 position;
layout (location = 2) in vec2 texcoords;
layout (location = 4) in vec4 color;

layout (location = 1) uniform vec2 scaler = vec2(1.0);
layout (location = 2) uniform vec2 adder = vec2(0.0);
layout (location = 3) uniform vec4 v_color = vec4(1.0);
layout (location = 4) uniform vec3 rotation = vec3(0.0);

layout (location = 0) out struct fge_out {
	vec4 f_color;
	vec2 f_texcoord;
} fout;

void main()
{
	fout.f_color = color * v_color;
	fout.f_texcoord = texcoords;
	vec3 rotter = vec3(rotation.xy, 0.0) * vec3(scaler, 1.0);
	vec3 prerot_pos = (position * vec3(scaler, 1.0)) + rotter;
	float cosrot = cos(rotation.z);
	float sinrot = sin(rotation.z);
	prerot_pos = vec3(prerot_pos.x * cosrot - prerot_pos.y * sinrot, prerot_pos.y * cosrot + prerot_pos.x * sinrot, prerot_pos.z);
	gl_Position = vec4(prerot_pos - rotter, 1.0) + vec4(adder, 0.0, 0.0);
}