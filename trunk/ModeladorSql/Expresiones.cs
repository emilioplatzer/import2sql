/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 02/05/2008
 * Time: 07:14 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;
using NUnit.Framework;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public class ListaElementos<TIE>:Lista<TIE> where TIE:IElemento{
		public ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(IElemento e in this){
				rta.AddRange(e.Tablas(queTablas));
			}
			return rta;
		}
	}
	public enum QueTablas{Aliasables, AlFrom};
	public class ConjuntoTablas:Conjunto<Tabla>{
		public ConjuntoTablas(){}
		public ConjuntoTablas(Tabla t):base(t){}
		public ConjuntoTablas(ConjuntoTablas t):base(t){}
	};
	public interface IElemento{
		string ToSql(BaseDatos db);
		ConjuntoTablas Tablas(QueTablas queTablas);
	}
	public interface IElementoTipado<T>:IElemento,IExpresion{
	}
	public interface IElementoNumerico<T>:IElementoTipado<T>{		
	}
	public interface IConCampos:IElemento{
		Lista<Campo> Campos();
	}
	public interface IElementoNombrado:IElemento{
		string NombreCampo{ get; }
	}
	/*
	public class Asignacion:IElemento{
		public Asig
		string NombreCampo{ get; }
		
	}
	*/
	public interface IExpresion:IElemento{
		bool CandidatoAGroupBy{ get; }
		bool EsAgrupada{ get; }
		IExpresion Expresion{ get; }
		int Precedencia{ get; }
	}
	public abstract class ElementoTipado<T>:IElementoTipado<T>,IExpresion{
		public abstract string ToSql(BaseDatos db);
		public virtual int Precedencia{ get{ return 9;} }
		public abstract ConjuntoTablas Tablas(QueTablas queTabla);
		public abstract bool CandidatoAGroupBy{ get; }
		public abstract bool EsAgrupada{ get; }
		public static implicit operator ElementoTipado<T>(T constante){
			return new Constante<T>(constante);
		}
		public IExpresion Expresion{
			get{ return this; }
		}
	}
	public interface IElementoLogico:IElementoTipado<bool>{
	}
	public abstract class ExpresionTipada<T1,T2,TR>:ElementoTipado<TR>{
		//protected IElementoTipado<T1> E1;
		//protected IElementoTipado<T2> E2;
		IExpresion e1;
		public IExpresion E1{ get{ return e1; } set{ e1=value.Expresion;}}
		IExpresion e2;
		public IExpresion E2{ get{ return e2; } set{ e2=value.Expresion;}}
		public override ConjuntoTablas Tablas(QueTablas queTabla){
			ConjuntoTablas rta=new ConjuntoTablas();
			if(E1!=null){ rta.AddRange(E1.Tablas(queTabla)); }
			if(E2!=null){ rta.AddRange(E2.Tablas(queTabla)); }
			return rta;
		}
		public override bool CandidatoAGroupBy{ 
			get{ 
				bool rta=true;
				if(E1!=null) rta=rta && E1.CandidatoAGroupBy;
				if(E2!=null) rta=rta && E2.CandidatoAGroupBy;
				return rta;
			}
		}
		public override bool EsAgrupada {
			get{ 
				bool rta=false;
				if(E1!=null) rta=rta || E1.CandidatoAGroupBy;
				if(E2!=null) rta=rta || E2.CandidatoAGroupBy;
				return rta;
			}
		}
		public string ToSqlInfijoConParentesisQueHaganFalta(BaseDatos db,string operador,int precedencia){
			var rta=new StringBuilder();
			if(E1.Precedencia<precedencia){
				rta.Append("("+E1.ToSql(db)+")");
			}else{
				rta.Append(E1.ToSql(db));
			}
			rta.Append(operador);
			if(E2.Precedencia<precedencia){
				rta.Append("("+E2.ToSql(db)+")");
			}else{
				rta.Append(E2.ToSql(db));
			}
			return rta.ToString();
		}
	}
	public class Constante<T>:ElementoTipado<T>{
		T Valor;
		public Constante(T Valor){
			this.Valor=Valor;
		}
		public override string ToSql(BaseDatos db){
			return db.StuffValor(Valor);
		}
		public static ConstanteNula<T> Nula{
			get { return new ConstanteNula<T>(); }
		}
		public override bool CandidatoAGroupBy{ 
			get{ return false;} 
		}
		public override bool EsAgrupada {
			get { return false; }
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
	}
	public class ConstanteNula<T>:ElementoTipado<T>{
		public override string ToSql(BaseDatos db){
			return "NULL";
		}
		public override bool CandidatoAGroupBy{ 
			get{ return false;} 
		}
		public override bool EsAgrupada {
			get { return false; }
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
	}
	public class Binomio<T>:ExpresionTipada<T,T,T>{
		public OperadorBinario Operador;
		public override string ToSql(BaseDatos db){
			return ToSqlInfijoConParentesisQueHaganFalta(db,db.OperadorToSql(Operador),Precedencia);
		}
		public override int Precedencia{ 
			get{ return BaseDatos.Precedencia(Operador);}
		}
	}
	public class Binomio3T<T1,T2,TR>:ExpresionTipada<T1,T2,TR>{
		public OperadorBinario Operador;
		public override string ToSql(BaseDatos db){
			return ToSqlInfijoConParentesisQueHaganFalta(db,db.OperadorToSql(Operador),Precedencia);
		}
		public override int Precedencia{ 
			get{ return BaseDatos.Precedencia(Operador);}
		}
	}
	public class BinomioRelacional<T>:ExpresionTipada<T,T,bool>{
		public OperadorBinarioRelacional Operador;
		public BinomioRelacional(IElementoTipado<T> E1,OperadorBinarioRelacional Operador,IElementoTipado<T> E2){	
			this.E1=E1;
			this.E2=E2;
			this.Operador=Operador;
		}
		public override string ToSql(BaseDatos db){
			return ToSqlInfijoConParentesisQueHaganFalta(db,db.OperadorToSql(Operador),Precedencia);
		}
		public override int Precedencia{ 
			get{ return BaseDatos.Precedencia(Operador);}
		}
	}
	public class OperacionSufijaLogica<T>:ExpresionTipada<T,T,bool>{
		OperadorSufijoLogico Operador;
		public OperacionSufijaLogica(IElementoTipado<T> E, OperadorSufijoLogico Operador){
			this.E1=E;
			this.Operador=Operador;
		}
		public override string ToSql(BaseDatos db){
			return E1.ToSql(db)+db.OperadorToSql(Operador);
		}
	}
	public class OperacionFuncion<T,TR>:ExpresionTipada<T,T,TR>{
		OperadorFuncion Operador;
		public OperacionFuncion(IElementoTipado<T> E, OperadorFuncion Operador){
			this.E1=E;
			this.Operador=Operador;
		}
		public override string ToSql(BaseDatos db){
			return db.OperadorToSqlPrefijo(Operador)+E1.ToSql(db)+db.OperadorToSqlSufijo(Operador);
		}
	}
	public class FuncionAgrupacion<T,TR>:ExpresionTipada<T,T,TR>{
		OperadorAgrupada Operador;
		public FuncionAgrupacion(IElementoTipado<T> E, OperadorAgrupada Operador){
			this.E1=E;
			this.Operador=Operador;
		}
		public override string ToSql(BaseDatos db){
			return db.OperadorToSqlPrefijo(Operador)+E1.ToSql(db)+db.OperadorToSqlSufijo(Operador);
		}
		public override bool CandidatoAGroupBy{ 
			get{ return false; }
		}
		public override bool EsAgrupada {
			get { return true; }
		}
	}
	public class FuncionCount:ElementoTipado<int>{
		IExpresion E;
		public FuncionCount(){
			
		}
		public FuncionCount(IExpresion E){
			this.E=E;
		}
		public override string ToSql(BaseDatos db){
			if(E==null){
				return "COUNT(*)";
			}else{
				return "COUNT("+E.ToSql(db)+")";
			}
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			if(E==null){
				return new ConjuntoTablas();
			}else{
				return E.Tablas(queTablas);
			}
		}
		public override bool CandidatoAGroupBy{ 
			get{ return false; }
		}
		public override bool EsAgrupada {
			get { return true; }
		}
		/*
		public int Precedencia {
			get { return 9; }
		}
		*/
	}
	public class SubSelectAgrupado<T>:IElementoTipado<T>{
		IElementoTipado<T> ExpresionBase;
		OperadorAgrupada Operador;
		Tabla TablaContexto;
		public SubSelectAgrupado(IElementoTipado<T> Expresion,OperadorAgrupada Operador,Tabla TablaContexto){
			this.ExpresionBase=Expresion;
			this.Operador=Operador;
			this.TablaContexto=TablaContexto;
		}
		public int Precedencia{
			get{ return 0; }
		}
		public bool CandidatoAGroupBy{
			get{ return false; }
		}
		public bool EsAgrupada {
			get{ return true; }
		}
		public string ToSql(BaseDatos db){
			throw new NotImplementedException();
		}
		public ConjuntoTablas Tablas(QueTablas queTablas){
			if(queTablas==QueTablas.Aliasables){
				ConjuntoTablas rta=new ConjuntoTablas();
				rta.AddRange(ExpresionBase.Tablas(queTablas));
				rta.Add(TablaContexto);
				return rta;
			}
			throw new NotImplementedException();
		}
		public IExpresion Expresion{
			get{ return this; }
		}
	}
	public static class ExtensionesLogicas{
		public static ElementoTipado<bool> And(this IElementoTipado<bool> E1, IElementoTipado<bool> E2){
			return new Binomio<bool>{E1=E1, Operador=OperadorBinario.And, E2=E2};
		}
		public static ElementoTipado<bool> Or(this IElementoTipado<bool> E1, IElementoTipado<bool> E2){
			return new Binomio<bool>{E1=E1, Operador=OperadorBinario.Or, E2=E2};
		}
	}
}
