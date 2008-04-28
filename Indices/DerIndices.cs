/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 24/04/2008
 * Time: 06:51 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;

using Modelador;
using BasesDatos;

namespace Indices
{
	/********************* CAMPOS ***************************/
	public class CampoProducto:CampoChar{ public CampoProducto():base(4){} };
	public class CampoEspecificacion:CampoChar{ public CampoEspecificacion():base(8){} };
	public class CampoVariedad:CampoChar{ public CampoVariedad():base(12){} };
	public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
	public class CampoAgrupacion:CampoChar{ public CampoAgrupacion():base(9){} };
	public class CampoGrupo:CampoChar{ public CampoGrupo():base(9){} };
	public class CampoPonderador:CampoReal{};
	public class CampoNivel:CampoEnteroOpcional{}
	public class CampoPrecio:CampoRealOpcional{};
	public class CampoIndice:CampoReal{};
	public class CampoFactor:CampoReal{};
	public class CampoPeriodo:CampoChar{ public CampoPeriodo():base(4+2){} }
	public class CampoVersion:CampoEntero{};
	public class CampoInformante:CampoEntero{};
	public class CampoTipo:CampoChar{ public CampoTipo():base(1){} };
	public class CampoNovedad<T>:CampoEnumerado<T>{};
	/********************* TABLAS ***************************/
	public class Productos:Tabla{
		[Pk] public CampoProducto cProducto;
		public CampoNombre	cNombreProducto;
		public double Ponderador(Grupos grupo){
			Grupos hoja=new Grupos();
			hoja.Leer(grupo.db,grupo.cAgrupacion,cProducto);
			return hoja.cPonderador.Valor/grupo.cPonderador.Valor;
		}
	}
	public class Agrupaciones:Tabla{
		[Pk] public CampoAgrupacion cAgrupacion;
		public CampoNombre cNombreAgrupacion;
	}
	public class Grupos:Tabla{
		[Pk] public CampoAgrupacion cAgrupacion;
		[Pk] public CampoGrupo cGrupo;
		public CampoNombre cNombreGrupo;
		[FkMixta("padre")] public CampoGrupo cGrupoPadre;
		public CampoPonderador cPonderador;
		public CampoNivel cNivel;
		public CampoLogico cEsProducto;
		[Fk] public Agrupaciones fkAgrupaciones;
		[FkMixta("padre")] public Grupos fkGrupoPadre;
		public ExpresionSql InPadresWhere(ExpresionSql e){
			return new ExpresionSql(
				this.cGrupoPadre,
				new LiteralSql(" IN (SELECT "),
				this.cGrupo,
				new LiteralSql(" FROM grupos WHERE "),
				e,
				new LiteralSql(")"));
		}
	}
	public class Numeros:Tabla{
		[Pk] public CampoEntero cNumero;
	}
	public class AuxGrupos:Tabla{
		[Pk] public CampoAgrupacion cAgrupacion;
		[Pk] public CampoGrupo cGrupo;
		public CampoPonderador cPonderadorOriginal;
		public CampoPonderador cSumaPonderadorHijos;	
	}
	public class Periodos:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		public CampoEntero cAno;
		public CampoEntero cMes;
		public Periodos(BaseDatos db,int ano, int mes){
			LeerNoPk(db,"ano",ano,"mes",mes);
		}
		public Periodos(){}
	}
	public class Calculos:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		public CampoLogico cEsPeriodoBase;
		[FkMixta("ant")] public CampoPeriodo cPeriodoAnterior;
		[Fk] public Periodos fkPeriodos;
		[FkMixta("ant")] public Periodos fkCalculoAnterior;
	}
	public class CalProd:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoProducto cProducto;
		public CampoPrecio cPromedio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Productos fkProductos;
	}
	public class CalGru:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoAgrupacion cAgrupacion;
		[Pk] public CampoGrupo cGrupo;
		public CampoIndice cIndice;
		public CampoFactor cFactor;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Grupos fkGrupos;
		public CalGru(){}
		public CalGru(BaseDatos db,Calculos p,Grupos g){
			Leer(db,p,g);
		}
		public CalGru(BaseDatos db,Calculos p,Agrupaciones a){
			Leer(db,p.cPeriodo,p.cCalculo,a.cAgrupacion,a.cAgrupacion);
		}
	}
	public class Informantes:Tabla{
		[Pk] public CampoInformante cInformante;
		public CampoNombre cNombreInformante;
		public CampoTipo cTipoInformante;
		public CampoNombre cRubro;
		public CampoNombre cCadena;
		public CampoNombre cDireccion;
	}
	public class ProdTipoInf:Tabla{
		[Pk] public CampoProducto cProducto;
		[Pk] public CampoTipo cTipoInf;
		public CampoPonderador cPonderadorTipoInf;
		[Fk] public Productos fkProductos;
	}
	public class CalTipoInf:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoProducto cProducto;
		[Pk] public CampoTipo cTipoInf;
		public CampoPrecio cPromedio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Productos fkProductos;
		[Fk] public ProdTipoInf fkProdTipoInf;
	}
	public class Especificaciones:Tabla{
		[Pk] public CampoEspecificacion cEspecificacion;
		public CampoNombre cNombreEspecificacion;
		public CampoRealOpcional cTamannoNormal;
		public CampoProducto cProducto;
		[Fk] public Productos fkProductos;
	}
	public class CalEsp:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoEspecificacion cEspecificacion;
		public CampoPrecio cPromedio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Especificaciones fkEspecificaciones;
	}
	public class CalEspInf:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoEspecificacion cEspecificacion;
		[Pk] public CampoInformante cInformante;
		public CampoPrecio cPromedio;
		public CampoEntero cAntiguedadConPrecio;
		public CampoEntero cAntiguedadSinPrecio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Especificaciones fkEspecificaciones;
	}
	public class NovEspInf:Tabla{
		public enum Estados{Alta,Baja,Reemplazo};
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoEspecificacion cEspecificacion;
		[Pk] public CampoInformante cInformante;
		public CampoNovedad<Estados> cEstado;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Especificaciones fkEspecificaciones;
		[Fk] public Informantes fkInformantes;
	}
	public class Variedades:Tabla{
		[Pk] public CampoVariedad cVariedad;
		public CampoNombre cNombreVariedad;
		public CampoRealOpcional cTamanno;
		public CampoNombre cUnidad;
		public CampoEspecificacion cEspecificacion;
		[Fk] public Especificaciones fkEspecificaciones;
	}
	public class RelVar:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVariedad cVariedad;
		[Pk] public CampoInformante cInformante;
		public CampoPrecio cPrecio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Variedades fkVariedades;
		[Fk] public Informantes fkInformantes;
	}
}
