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
	public class Diccionario<TKey, TValue>:System.Collections.Generic.Dictionary<TKey, TValue>{};
	public class Lista<T>:System.Collections.Generic.List<T>{};
	public abstract class Sentencia{
		Lista<Sqlizable> ParteWhere=new Lista<Sqlizable>();
		protected System.Collections.Generic.Dictionary<string, string> AliasTablas=new System.Collections.Generic.Dictionary<string, string>();
		protected Lista<Tabla> TablasUsadas;
		protected virtual void ClonarMiembros(Sentencia s){
			ParteWhere=new Lista<Sqlizable>();
			ParteWhere.AddRange(s.ParteWhere);
			AliasTablas=new System.Collections.Generic.Dictionary<string, string>(s.AliasTablas);
			if(s.TablasUsadas==null){
				TablasUsadas=null;
			}else{
				TablasUsadas=new Lista<Tabla>();
				TablasUsadas.AddRange(s.TablasUsadas);
			}
		}
		public abstract Lista<Sqlizable> Partes();
		public abstract Lista<Tabla> Tablas();
		public Sentencia Where(ExpresionSql expresion){
			if(ParteWhere.Count>0){
				ParteWhere.Add(new LiteralSql("\n AND "));
			}
			ParteWhere.Add(expresion);
			return this;
		}
		public Lista<Sqlizable> PartesWhere(){
			Lista<Sqlizable> rta=new Lista<Sqlizable>();
			if(ParteWhere.Count>0){
				rta.Add(new LiteralSql("\n WHERE "));
				rta.AddRange(ParteWhere);
			}
			return rta;
		}
		protected void RegistrarTablas(Campo c){
			ExpresionSql expresion=c.ExpresionBase;
			if(expresion!=null){
				RegistrarTablas(expresion);
			}else{
				RegistrarTabla(c.TablaContenedora);
			}
		}
		protected void RegistrarTablas(Lista<Campo> Campos){
			foreach(Campo c in Campos){
				RegistrarTablas(c);
			}
		}
		protected void RegistrarTablas(ExpresionSql expresion){
			RegistrarTablas(expresion.Partes);
		}
		protected void RegistrarTablas(Lista<Sqlizable> Partes){
			foreach(Sqlizable s in Partes){
				if(s is Campo){
					RegistrarTablas(s as Campo);
				}else if(s is ExpresionSql){
					RegistrarTablas(s as ExpresionSql);
				}else if(s is ValorSql<Campo>){
					RegistrarTablas((s as ValorSql<Campo>).Valor);
				}else if(s is ValorSql<ExpresionSql>){
					RegistrarTablas((s as ValorSql<ExpresionSql>).Valor);
				}else if(s is SentenciaUpdate.Sets){
					RegistrarTablas((s as SentenciaUpdate.Sets).ValorAsignar);
				}else if(s is ExpresionSql.SelectSuma){
					AsignarAlias((s as ExpresionSql.SelectSuma).CampoSumar.TablaContenedora);
				}
			}
		}
		protected void AsignarAlias(Tabla t){
			int Largo=1;
			int Sufijo=0;
			string Alias=t.NombreTabla.Substring(0,Largo);
			while(AliasTablas.ContainsKey(Alias)){
				if(Sufijo==0 && Largo<t.NombreTabla.Length){
					Largo++;
				}else{
					Largo=1;
					Sufijo++;
				}
				if(Sufijo==0){
					Alias=t.NombreTabla.Substring(0,Largo);
				}else{
					Alias=t.NombreTabla.Substring(0,Largo)+Sufijo.ToString();
				}
			}
			AliasTablas.Add(Alias,t.NombreTabla);
			t.Alias=Alias;
		}
		protected void RegistrarTabla(Tabla t){
			if(t!=null){
				if(TablasUsadas.IndexOf(t)<0){
					TablasUsadas.Add(t);
					AsignarAlias(t);
				}
			}
		}
		public virtual Sentencia Clonate(){
			Sentencia rta=(Sentencia) this.MemberwiseClone();
			rta.ClonarMiembros(this);
			return rta;
		}
	}
	public class SentenciaUpdate:Sentencia{
		public Tabla TablaBase;
		public Lista<Sqlizable> ParteSet=new Lista<Sqlizable>();
		public SentenciaUpdate(Tabla tabla,Sets primerSet,params Sets[] sets){
			TablaBase=tabla;
			ParteSet.Add(primerSet);
			ParteSet.AddRange(sets);
			/*
			foreach(Sets s in sets){
				ParteSet.Add(s);
			}
			*/
		}
		public class Sets:Sqlizable{
			public Campo CampoAsignado;
			public ExpresionSql ValorAsignar;
			public Sets(Campo CampoAsignado,ExpresionSql ValorAsignar){
				this.CampoAsignado=CampoAsignado;
				this.ValorAsignar=ValorAsignar;
			}
			public override string ToSql(BaseDatos db)
			{
				return CampoAsignado.ToSql(db)+"="+ValorAsignar.ToSql(db);
			}
			public override bool CandidatoAGroupBy{ 
				get{
					Assert.Fail("No debería preguntar si un Set tiene variables");
					return false;
				}
			}
		}
		public override Lista<Sqlizable> Partes(){
			Lista<Sqlizable> todas=new Lista<Sqlizable>();
			todas.AddRange(ParteSet);
			todas.AddRange(PartesWhere());
			return todas;
		}
		public override Lista<Tabla> Tablas(){
			if(TablasUsadas==null){
				TablasUsadas=new Lista<Tabla>();
				RegistrarTabla(TablaBase);
				RegistrarTablas(ParteSet);
				RegistrarTablas(PartesWhere());
			}
			return TablasUsadas;
		}
	}
	public class ParteSeparadora{
		string Comenzador;
		string Separador;
		bool esPrimero=true;
		public ParteSeparadora(string Comenzador,string Separador){
			this.Comenzador=Comenzador;
			this.Separador=Separador;
		}
		public ParteSeparadora(string Separador){
			this.Separador=Separador;
		}
		public void AgregarEn(Lista<Sqlizable> Partes,params Sqlizable[] Parte){
			if(esPrimero){
				if(Comenzador!=null){
					Partes.Add(new LiteralSql(Comenzador));
				}
				esPrimero=false;
			}else{
				Partes.Add(new LiteralSql(Separador));
			}
			Partes.AddRange(Parte);
		}
	}
	public class SentenciaSelect:Sentencia{
		protected Lista<Campo> Campos=new Lista<Campo>();
		public SentenciaSelect(params Campo[] Campos){
			this.Campos.AddRange(Campos);
		}
		public override Lista<Sqlizable> Partes(){
			Lista<Sqlizable> todas=new Lista<Sqlizable>();
			Lista<Sqlizable> groupBy=new Lista<Sqlizable>();
			bool tieneAgrupados=false;
			todas.Add(new LiteralSql("SELECT "));
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				ParteSeparadora sepGB=new ParteSeparadora("\n GROUP BY ",", ");
				foreach(Campo c in Campos){
					ExpresionSql expresion=c.ExpresionBase;
					if(expresion!=null){
						if(c.ExpresionBase.TipoAgrupada){
							tieneAgrupados=true;
						}else if(expresion.CandidatoAGroupBy){
							sepGB.AgregarEn(groupBy,expresion);
						}
						coma.AgregarEn(todas,expresion,new LiteralSql(" AS "),new CampoReceptorInsert(c));
					}else{
						sepGB.AgregarEn(groupBy,c);
						coma.AgregarEn(todas,c);
					}
				}
			}
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				todas.Add(new LiteralSql("\n FROM "));
				foreach(Tabla t in TablasUsadas){
					coma.AgregarEn(todas,t);
				}
			}
			todas.AddRange(PartesWhere());
			if(tieneAgrupados){
				todas.AddRange(groupBy);
			}
			return todas;
		}
		public override Lista<Tabla> Tablas(){
			if(TablasUsadas==null){
				TablasUsadas=new Lista<Tabla>();
				RegistrarTablas(Campos);
				RegistrarTablas(PartesWhere());
			}
			return TablasUsadas;
		}
	}
	public class CampoReceptorInsert:Sqlizable{
		Campo CampoSinAlias;
		public CampoReceptorInsert(Campo CampoSinAlias){
			this.CampoSinAlias=CampoSinAlias;
		}
		public override string ToSql(BaseDatos db)
		{
			return db.StuffCampo(CampoSinAlias.NombreCampo);
		}
		public override bool CandidatoAGroupBy{ get{return CampoSinAlias.CandidatoAGroupBy;} }
	}
	public class SentenciaInsert:SentenciaSelect{
		Tabla TablaBase;
		enum ConValuesOSelect {ConValues, ConSelect};
		ConValuesOSelect conQue;
		public SentenciaInsert(Tabla TablaBase){
			this.TablaBase=TablaBase;	
		}
		public SentenciaInsert Select(params Campo[] Campos){
			conQue=ConValuesOSelect.ConSelect;
			this.Campos.AddRange(Campos);
			return this;
		}
		public Sentencia Valores(params Campable[] CamposConValores){
			conQue=ConValuesOSelect.ConValues;
			foreach(Campable cc in CamposConValores){
				foreach(Campo c in cc.Campos()){
					if(TablaBase.TieneElCampo(c)){
						this.Campos.Add(c);
					}
				}
			}
			return this;
		}
		public override Lista<Sqlizable> Partes(){
			Lista<Sqlizable> todas=new Lista<Sqlizable>();
			Lista<Sqlizable> valores=new Lista<Sqlizable>();
			todas.Add(new LiteralSql("INSERT INTO "));
			todas.Add(TablaBase);
			TablaBase.Alias=null;
			ParteSeparadora coma=new ParteSeparadora(" (",", ");
			ParteSeparadora vcoma=new ParteSeparadora("VALUES (",", ");
			foreach(Campo c in Campos){
				coma.AgregarEn(todas,new CampoReceptorInsert(c));
				if(conQue==ConValuesOSelect.ConValues){
					if(c.ExpresionBase!=null){
						vcoma.AgregarEn(valores,c.ExpresionBase);
					}else{
						vcoma.AgregarEn(valores,new ValorSql<object>(c.ValorSinTipo));
					}
				}
			}
			todas.Add(new LiteralSql(") "));
			if(conQue==ConValuesOSelect.ConSelect){
				todas.AddRange(base.Partes());
			}else{
				todas.AddRange(valores);
				todas.Add(new LiteralSql(")"));
			}
			return todas;
		}
	}
	public class Ejecutador:BasesDatos.EjecutadorSql{
		Lista<Campo> CamposContexto=new Lista<Campo>();
		public Ejecutador(BaseDatos db,params Tabla[] TablasContexto)
			:base(db)
		{
			foreach(Tabla t in TablasContexto){
				foreach(Campo c in t.CamposPk()){
					if(c.ValorSinTipo!=null){
						CamposContexto.Add(c);
					}
				}
			}
		}
		public void Ejecutar(Sentencia s){
			base.ExecuteNonQuery(Dump(s));
		}
		public string Dump(Sentencia laSentencia){
			Sentencia s=laSentencia.Clonate();
			StringBuilder rta=new StringBuilder();
			if(s is SentenciaUpdate){
				rta.Append("UPDATE ");
				SentenciaUpdate su=s as SentenciaUpdate;
				Lista<Tabla> suTablas=su.Tablas();
				if(suTablas.Count<=1 || !db.UpdateConJoin){
					su.TablaBase.Alias=null;
				}
				rta.Append(su.TablaBase.ToSql(db));
				string prefijoSet="";
				string sufijoSet="";
				if(db.UpdateConJoin){
					foreach(Tabla t in su.Tablas()){
						if(t!=su.TablaBase){
							if(t.TablaRelacionada!=null && su.Tablas().Contains(t.TablaRelacionada)){
								rta.Append(" INNER JOIN "+t.ToSql(db)+" ON ");
								Separador and=new Separador(" AND ");
								foreach(Campo c in t.CamposPk()){
									rta.Append(and+t.CamposRelacionadosFk[c].UnicoCampo().Igual(c).ToSql(db));
								}
								rta.Append("\n");
							}
						}
					}
					foreach(Tabla t in s.Tablas()){
						if(!t.LiberadaDelContextoDelEjecutador){
							foreach(Campo c in CamposContexto){
								if(t.TieneElCampo(c)){
									s.Where(t.CampoIndirecto(c).Igual(c.ValorSinTipo));
								}
							}
						}
					}
				}else{
					StringBuilder parteFrom=new StringBuilder();
					StringBuilder parteWhere=new StringBuilder();
					Separador and=new Separador(" WHERE "," AND ");
					Separador coma=new Separador(" FROM ",", ");
					foreach(Tabla t in su.Tablas()){
						if(t!=su.TablaBase){
							if(t.TablaRelacionada!=null && su.Tablas().Contains(t.TablaRelacionada)){
								parteFrom.Append(coma+t.ToSql(db));
								foreach(Campo c in t.CamposPk()){
									parteWhere.Append(and+c.Igual(t.CamposRelacionadosFk[c]).ToSql(db));
								}
							}
						}
					}
					if(parteFrom.Length>0){
						prefijoSet="(SELECT ";
						sufijoSet=parteFrom.ToString()+parteWhere.ToString()+")";
					}
					if(!su.TablaBase.LiberadaDelContextoDelEjecutador){
						foreach(Campo c in CamposContexto){
							if(su.TablaBase.TieneElCampo(c)){
								s.Where(su.TablaBase.CampoIndirecto(c).Igual(c.ValorSinTipo));
							}
						}
					}
				}
				Separador setComa=new Separador(" SET ",",\n ");
				foreach(SentenciaUpdate.Sets p in su.ParteSet){
					rta.Append(setComa+p.CampoAsignado.ToSql(db)+"="+prefijoSet+p.ValorAsignar.ToSql(db)+sufijoSet);
				}
				foreach(Sqlizable p in su.PartesWhere()){
					rta.Append(p.ToSql(db));
				}
			}else{
				foreach(Tabla t in s.Tablas()){
					if(t.TablaRelacionada!=null && s.Tablas().Contains(t.TablaRelacionada)){
					// if(t.TablaRelacionada!=null){
						foreach(Campo c in t.CamposPk()){
							s.Where(c.Igual(t.CamposRelacionadosFk[c]));
						}
					}
				}
				foreach(Tabla t in s.Tablas()){
					if(!t.LiberadaDelContextoDelEjecutador){
						foreach(Campo c in CamposContexto){
							if(t.TieneElCampo(c)){
								s.Where(t.CampoIndirecto(c).Igual(c.ValorSinTipo));
							}
						}
					}
				}
				foreach(Sqlizable p in s.Partes()){
					rta.Append(p.ToSql(db));
				}
			}
			rta.Append(";\n");
			return rta.ToString();
		}
	}
	public class ExpresionSql:Sqlizable{
		public bool TipoAgrupada=false;
		bool candidatoAGroupBy=false;
		public override bool CandidatoAGroupBy{ get{ return candidatoAGroupBy; } }
		public Lista<Sqlizable> Partes=new Lista<Sqlizable>();
		void CalcularTipo(){
			foreach(Sqlizable p in Partes){
				if(p is ExpresionSql){
					TipoAgrupada=TipoAgrupada || (p as ExpresionSql).TipoAgrupada;
				}
				candidatoAGroupBy=candidatoAGroupBy || p.CandidatoAGroupBy;
			}
		}
		public ExpresionSql(params Sqlizable[] Partes){
			this.Partes.AddRange(Partes);
			CalcularTipo();
		}
		public ExpresionSql(Lista<Sqlizable> Partes){
			this.Partes=Partes;
			CalcularTipo();
		}
		public virtual ExpresionSql And(ExpresionSql otra){
			Lista<Sqlizable> nueva=new Lista<Sqlizable>();
			nueva.AddRange(Partes);
			nueva.Add(new LiteralSql("\n AND "));
			nueva.AddRange(otra.Partes);
			return new ExpresionSql(nueva);
		}
		public virtual ExpresionSql Or(ExpresionSql otra){
			Lista<Sqlizable> nueva=new Lista<Sqlizable>();
			nueva.Add(new LiteralSql("("));
			nueva.AddRange(Partes);
			nueva.Add(new LiteralSql(" OR "));
			nueva.AddRange(otra.Partes);
			nueva.Add(new LiteralSql(")"));
			return new ExpresionSql(nueva);
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			foreach(Sqlizable s in Partes){
				rta.Append(s.ToSql(db));
			}
			return rta.ToString();
		}
		public Campo UnicoCampo(){
			Assert.AreEqual(1,Partes.Count);
			return (Campo)Partes[0];
		}
		public ExpresionSql Operado<T>(string OperadorTextual,T expresion){
			return new ExpresionSql(this,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion));
		}
		public ExpresionSql Dividido<T2>(T2 Valor){
			return Operado<T2>("/",Valor);
		}
		public static ExpresionSql Agrupada<T>(string Operador, T expresion){
			/*
			Lista<Sqlizable> nuevasPartes=new Lista<Sqlizable>();
			nuevasPartes.Add(Operador+"(");
			nuevasPartes.AddRange(expresion.Partes);
			nuevasPartes.Add(")");
			ExpresionSql nueva=new ExpresionSql(nuevasPartes);
			*/
			ExpresionSql nueva=new ExpresionSql(new LiteralSql(Operador+"("),new ValorSql<T>(expresion),new LiteralSql(")"));
			nueva.TipoAgrupada=true;
			return nueva;
		}
		public static ExpresionSql Sum<T>(T expresion){
			return Agrupada("SUM",expresion);
		}
		public class SelectSuma:Sqlizable{
			Tabla TablaBase;
			public Campo CampoSumar;
			// ExpresionSql ExpresionWhere;
			/*
			public SelectSuma(Tabla TablaBase,Campo CampoSumar,ExpresionSql ExpresionWhere){
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
			}
			*/
			public SelectSuma(Tabla TablaBase,Campo CampoSumar)
			{
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
			}
			public override string ToSql(BaseDatos db)
			{
				// return "";
				StringBuilder rta=new StringBuilder();
				if(db.UpdateSelectSumViaDSum){
					Tabla TablaSumandis=CampoSumar.TablaContenedora;
					rta.Append("DSum('"+db.StuffCampo(CampoSumar.NombreCampo)+"','"
					           +db.StuffTabla(TablaSumandis.NombreTabla));
					if(TablaSumandis==TablaBase.TablaRelacionada){
						rta.Append("','");
						Separador and=new Separador(" AND ");
						foreach(Campo c in TablaBase.CamposPk()){
							rta.Append(and+db.StuffCampo(TablaBase.CamposRelacionadosFk[c].UnicoCampo().NombreCampo)+"=''' & "+c.ToSql(db)+" & '''");
						}
					}
					rta.Append("')");
					return rta.ToString();
				}else{
					Tabla TablaSumandis=CampoSumar.TablaContenedora;
					rta.Append("(SELECT SUM("+CampoSumar.ToSql(db)+") FROM "
					           +TablaSumandis.ToSql(db));
					if(TablaSumandis==TablaBase.TablaRelacionada){
						Separador and=new Separador(" WHERE "," AND ");
						foreach(Campo c in TablaBase.CamposPk()){
							rta.Append(and+TablaBase.CamposRelacionadosFk[c].ToSql(db)+"="+c.ToSql(db));
						}
					}
					rta.Append(")");
					return rta.ToString();
				}
			}
			public static implicit operator ExpresionSql(SelectSuma ss){
				return new ExpresionSql(ss);
			}
			public override bool CandidatoAGroupBy{ 
				get{
					return false;
				} 
			}
		}
	}
}
