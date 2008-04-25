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
		[FkMixta("ant")] public CampoPeriodo cPeriodoAnterior;
		public CampoEntero cAno;
		public CampoEntero cMes;
		[FkMixta("ant")] public Periodos fkPeriodoAnterior;
		public Periodos(BaseDatos db,int ano, int mes){
			LeerNoPk(db,"ano",ano,"mes",mes);
		}
		public Periodos(){}
		public Periodos CrearProximo(){
			int ano=cAno.Valor;
			int mes=cMes.Valor+1;
			if(mes==13){
				mes=1; 
				ano++;
			}
			Periodos p=new Periodos();
			using(Insertador ins=p.Insertar(db)){
				p.cPeriodo[ins]=ano.ToString()+((int)mes).ToString("00");
				p.cAno[ins]=ano;
				p.cMes[ins]=mes;
				p.cPeriodoAnterior[ins]=cPeriodo;
			}
			return new Periodos(db,ano,mes);
		}
	}
	public class Calculos:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		public CampoLogico cEsPeriodoBase;
		[Fk] public Periodos fkPeriodos;
		public Calculos CrearProximo(){
			Periodos p=new Periodos();
			p.Leer(db,cPeriodo);
			Periodos pProx=p.CrearProximo();
			Calculos c=new Calculos();
			using(Insertador ins=c.Insertar(db)){
				c.cPeriodo[ins]=pProx.cPeriodo.Valor;
				c.cCalculo[ins]=cCalculo.Valor;
			}
			c.Leer(db,pProx.cPeriodo.Valor,cCalculo.Valor);
			return c;
		}
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
		public CampoReal cTamannoNormal;
		public CampoProducto cProducto;
		[Fk] public Productos fkProductos;
	}
	public class CalEsp:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoProducto cProducto;
		[Pk] public CampoTipo cTipoInf;
		[Pk] public CampoEspecificacion cEspecificacion;
		public CampoPrecio cPromedio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Productos fkProductos;
		[Fk] public ProdTipoInf fkProdTipoInf;
		[Fk] public Especificaciones fkEspecificaciones;
	}
	public class Variedades:Tabla{
		[Pk] public CampoVariedad cVariedad;
		public CampoNombre cNombreVariedad;
		public CampoReal cTamanno;
		public CampoNombre cUnidad;
		public CampoEspecificacion cEspecificacion;
		[Fk] public Especificaciones fkEspecificaciones;
	}
	public class RelVar:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		[Pk] public CampoVariedad cVariedad;
		[Pk] public CampoInformante cInformante;
		public CampoPrecio cPrecio;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Variedades fkVariedades;
		[Fk] public Informantes fkInformantes;
	}
	public class CalVar:Tabla{
		[Pk] public CampoPeriodo cPeriodo;
		[Pk] public CampoVersion cCalculo;
		public CampoProducto cProducto;
		public CampoTipo cTipoInf;
		public CampoEspecificacion cEspecificacion;
		[Pk] public CampoVariedad cVariedad;
		[Pk] public CampoInformante cInformante;
		public CampoPrecio cPrecio;
		public CampoTipo cTipoImputacion;
		public CampoEntero cAntiguedad;
		[Fk] public Periodos fkPeriodos;
		[Fk] public Calculos fkCalculos;
		[Fk] public Productos fkProductos;
		[Fk] public ProdTipoInf fkProdTipoInf;
		[Fk] public Especificaciones fkEspecificaciones;
		[Fk] public Variedades fkVariedades;
		[Fk] public Informantes fkInformantes;
	}
}
