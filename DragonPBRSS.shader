Shader "Demo/DragonPBR1"
{
	Properties
	{
		_Albedo_Rough("Albedo_Rough", 2D) = "white" {}
		_Metallic("Metallic", Range( 0 , 1)) = 0
		_Emission("Emission", 2D) = "white" {}
		_WingSSS_Scale("WingSSS_Scale", Range( 0 , 10)) = 0
		_Wing_Frequency("Wing_Frequency", Range( 0 , 50)) = 0
		_Wing_Sine("Wing_Sine", Range( 0 , 10)) = 0
		_Wing_Noise("Wing_Noise", Range( 0 , 10)) = 0
		_Body_Scale("Body_Scale", Range( 0 , 10)) = 0
		_Body_Frequency("Body_Frequency", Range( 0 , 50)) = 0
		_Body_Sine("Body_Sine", Range( 0 , 10)) = 0
		_Body_Noise("Body_Noise", Range( 0 , 10)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Albedo_Rough;
		uniform float4 _Albedo_Rough_ST;
		uniform sampler2D _Emission;
		uniform float4 _Emission_ST;
		uniform float _WingSSS_Scale;
		uniform float _Wing_Frequency;
		uniform float _Wing_Sine;
		uniform float _Wing_Noise;
		uniform float _Body_Scale;
		uniform float _Body_Frequency;
		uniform float _Body_Sine;
		uniform float _Body_Noise;
		uniform float _Metallic;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Albedo_Rough = i.uv_texcoord * _Albedo_Rough_ST.xy + _Albedo_Rough_ST.zw;
			float4 tex2DNode1 = tex2D( _Albedo_Rough, uv_Albedo_Rough );
			o.Albedo = tex2DNode1.rgb;
			float2 uv_Emission = i.uv_texcoord * _Emission_ST.xy + _Emission_ST.zw;
			float4 tex2DNode4 = tex2D( _Emission, uv_Emission );
			float ifLocalVar26 = 0;
			if( tex2DNode4.a > 0.5 )
				ifLocalVar26 = 1.0;
			float mulTime18 = _Time.y * 1;
			float2 temp_cast_1 = (( sin( ( mulTime18 / _Wing_Frequency ) ) * _Wing_Sine )).xx;
			float2 uv_TexCoord21 = i.uv_texcoord * float2( 1,1 ) + temp_cast_1;
			float simplePerlin2D13 = snoise( uv_TexCoord21 );
			float ifLocalVar29 = 0;
			if( tex2DNode4.a < 0.5 )
				ifLocalVar29 = 1.0;
			float mulTime39 = _Time.y * 1;
			float2 temp_cast_2 = (( abs( sin( ( mulTime39 / _Body_Frequency ) ) ) * _Body_Sine )).xx;
			float2 uv_TexCoord44 = i.uv_texcoord * float2( 1,1 ) + temp_cast_2;
			float simplePerlin2D45 = snoise( uv_TexCoord44 );
			float clampResult51 = clamp( ( _Body_Scale + ( simplePerlin2D45 * _Body_Noise ) ) , 0 , 10 );
			o.Emission = ( tex2DNode4 * ( ( ifLocalVar26 * ( _WingSSS_Scale + ( simplePerlin2D13 * _Wing_Noise ) ) ) + ( ifLocalVar29 * clampResult51 ) ) ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = ( 1.0 - tex2DNode1.a );
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}

