Shader "Custom/FlatTransparent_Fog" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_FogColor ("Fog", Color) = (.3,.3,.3,1)
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
		uniform fixed4 _FogColor;
		uniform half _Density;

		struct vIn{
			half4 vertex : POSITION;
			fixed4 color: COLOR;
			half4 worldPos: TEXCOORD0;
		};
		struct vOut{
			half4 pos : SV_POSITION;
			half4 wpos: TEXCOORD0;
			fixed4 col: COLOR;
		};

		//vertex function
		vOut vert(vIn i){
			vOut o;

			o.pos = mul(UNITY_MATRIX_MVP,i.vertex);
			o.wpos = mul(_Object2World,i.vertex);
			o.col=i.color;

			return o;
		}

		//fragment function
		float4 frag(vOut i) : COLOR {
			half fogPow = max(1/exp(pow((max(i.wpos.z,0))*_Density,1)),.15);
			return fixed4(lerp( _FogColor, i.col*_Color, fogPow*2).xyz,fogPow);
			//return fixed4(_Color.xyz,fogPow);
			//return lerp( _FogColor, _Color, fogPow);
		}

		ENDCG
	}
	}
	//FallBack "Diffuse"
}
