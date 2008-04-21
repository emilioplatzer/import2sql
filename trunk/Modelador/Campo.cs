/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 04/04/2008
 * Time: 05:38 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Reflection;
using System.Text;
using System.Data;
using System.Data.Common;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using Modelador;

namespace Modelador
{
	public abstract class Campo:Sqlizable{
		public string Nombre;
		public string NombreCampo;
		public abstract string TipoCampo{ get; }
		public bool EsPk;
		public Tabla TablaContenedora;
		public Campo(){
		}
		public object this[InsertadorSql ins]{
			set{
				if(value is Campo){
					ins[this.NombreCampo]=(value as Campo).ValorSinTipo;
				}else{
					ins[this.NombreCampo]=value;
				}
			}
		}
		public abstract object ValorSinTipo{ get; }
		public abstract void AsignarValor(object valor);
		/*
		public void Entablar(Tabla tabla){ // marcarlo como perteneciente a la tabla
			TablaContenedora=tabla;
		}
		*/
		public virtual ExpresionSql EsNulo(){
			return new ExpresionSql(this,new LiteralSql(" IS NULL"));
		}
		public override string ToSql(BaseDatos db)
		{
			if(this.TablaContenedora==null || this.TablaContenedora.Alias==null){
				return db.StuffCampo(this.NombreCampo);
			}else{
				return this.TablaContenedora.Alias+"."+db.StuffCampo(NombreCampo);
			}
		}
		public override bool TieneVariables{ 
			get{
				if(ExpresionBase!=null){
					return ExpresionBase.TieneVariables;
				}
				return true;
			}
		}
		public ExpresionSql Operado<T>(string OperadorTextual,T expresion){
			return new ExpresionSql(this,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion));
		}
		public ExpresionSql Igual<T>(T expresion){
			return Operado<T>("=",expresion);
		}
		public ExpresionSql Distinto<T>(T expresion){
			return Operado<T>("<>",expresion);
		}
		public ExpresionSql ExpresionBase;
		public Campo Es(ExpresionSql expresion){
			ExpresionBase=expresion;
			return this;
		}
		public Campo Es(Campo campo){
			return Es(new ExpresionSql(campo));
		}
	}
	public class CampoTipo<T>:Campo{
		protected T valor;
		public virtual T Valor{ get{ return valor;} set{ valor=value; } }
		public override object ValorSinTipo{ get{ return valor;} }
		public override string TipoCampo{ 
			get {
				if(valor is int || valor is int?){
					return "integer";
				}else if(valor is string){
					return "varchar";
				}else if(valor is double){
					return "double precision";
				}else{
					return typeof(T).Name; 
				}
			} 
		}
		public CampoTipo()
		{	
		}
		public override void AsignarValor(object valor){
			if(valor is DBNull){
				valor=null;
			}
			this.valor=(T)valor;
		}
		public virtual SentenciaUpdate.Sets Set(T valor){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(new ValorSql<T>(valor)));
		}
		public virtual SentenciaUpdate.Sets Set(ExpresionSql expresion){
			return new SentenciaUpdate.Sets(this,expresion);
		}
		public virtual SentenciaUpdate.Sets Set(Campo campo){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(campo));
		}
		public virtual SentenciaUpdate.Sets SetNull(){
			return new SentenciaUpdate.Sets(this,new ExpresionSql(new ValorSqlNulo()));
		}
		public Campo Es(T valor){
			return Es(new ExpresionSql(new ValorSql<T>(valor)));
		}
	}
	public class CampoPkTipo<T>:CampoTipo<T>{
		public CampoPkTipo()
		{	
			EsPk=true;
		}
	}
	public class CampoDestino<T>:CampoNumericoTipo<T>{
		public CampoDestino(string NombreCampo){
			this.NombreCampo=NombreCampo;
		}
	}
	public class CampoNumericoTipo<T>:CampoTipo<T>{
		public Campo EsExpresionAgrupada(string operador,ExpresionSql expresion){
			Lista<Sqlizable> nueva=new Lista<Sqlizable>();
			nueva.Add(new LiteralSql(operador+"("));
			nueva.AddRange(expresion.Partes);
			nueva.Add(new LiteralSql(")"));
			ExpresionBase=new ExpresionSql(nueva);
			ExpresionBase.TipoAgrupada=true;
			return this;	
		}
		public Campo EsSuma(ExpresionSql expresion){
			return EsExpresionAgrupada("SUM",expresion);
		}
		public Campo EsSuma(Campo campo){
			return EsExpresionAgrupada("SUM",new ExpresionSql(campo));
		}
		public ExpresionSql Mas<T2>(T2 Valor){
			return Operado<T2>("+",Valor);
		}
		public ExpresionSql Por<T2>(T2 Valor){
			return Operado<T2>("*",Valor);
		}
		public ExpresionSql Dividido<T2>(T2 Valor){
			return Operado<T2>("/",Valor);
		}
	}
	public class CampoEntero:CampoNumericoTipo<int>{
		public override string TipoCampo{ 
			get { return "integer"; }
		}
	};
	public class CampoEnteroOpcional:CampoTipo<int?>{
		public override string TipoCampo{ 
			get { return "integer"; }
		}
	};
	public class CampoChar:CampoTipo<string>{
		public int Largo;
		protected CampoChar(int largo){
			this.Largo=largo;	
		}
		public override string TipoCampo{ 
			get { return "varchar("+Largo.ToString()+")"; }
		}
		public ExpresionSql Concatenado<T>(T expresion){
			return new ExpresionSql(new OperadorConcatenacionIzquierda()
			                        ,this,new OperadorConcatenacionMedio()
			                        ,new ValorSql<T>(expresion),new OperadorConcatenacionDerecha());
		}
	};
	public class CampoReal:CampoNumericoTipo<double>{};
	public class CampoLogico:CampoChar{
		public CampoLogico():base(1){}
	}
	public abstract class AplicadorCampo:System.Attribute{
	   	public abstract void Aplicar(ref Campo campo);
	}
	public class Pk:AplicadorCampo{
	   	public override void Aplicar(ref Campo campo){
	   		campo.EsPk=true;
	    }
	}
}
