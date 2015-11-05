Shader "Custom/PolarFlatTex" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_P_Offset ("PolarOffset", Vector) = (0,0,0,0)
	}
	SubShader {
	Tags {
		"Queue" = "Transparent"
		"IgnoreProjector"="True"
	}
	Pass{
		Tags {
			"LightMode"="Always"
		}

		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		Zwrite Off

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag

		//unity Defs
		uniform fixed4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform fixed4 _P_Offset;

		const float pi = 3.14159;

		struct vin{
			half4 vertex : POSITION;
			half4 texcoord: TEXCOORD0;
		};

		struct v2f{
			half4 pos : SV_POSITION;
			half4 tex: TEXCOORD0;
		};

		//vertex shader
		v2f vert(vin v){
			v2f o;

			o.pos= mul(UNITY_MATRIX_MVP,v.vertex);
			o.tex=v.texcoord;
			//o.worldPos = mul(_Object2World,v.vertex);
			return o;
		}

		//fragment function
		float4 frag(v2f i) : COLOR {
			//Get uv relative to center of the image
			fixed2 tex = i.tex.xy - .5;
			//Offset by texture offset
			tex+=_MainTex_ST.zw;
			//Calculate polar coordinates
			fixed2 newUV = fixed2( atan2(tex.y,tex.x)/(pi*2), length(tex));
			//Apply polar offset and get color
			fixed4 texColor = (fixed4) tex2D( _MainTex, newUV+_P_Offset.xy);
			return texColor*_Color;
		}
		ENDCG
	}
	}
	FallBack "Diffuse"
}
