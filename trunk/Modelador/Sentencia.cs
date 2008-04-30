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
	public enum QueTablas{Aliasables, AlFrom};
	public class Diccionario<TKey, TValue>:System.Collections.Generic.Dictionary<TKey, TValue>{};
	public class Lista<T>:System.Collections.Generic.List<T>{};
	public class ConjuntoTablas:Conjunto<Tabla>{
		public ConjuntoTablas(){}
		public ConjuntoTablas(Tabla t):base(t){}
		public ConjuntoTablas(ConjuntoTablas t):base(t){}
	};
	/*
	public class ConjuntoTablas:System.Collections.Generic.Dictionary<Tabla, int>{
		ConjuntoTablas AddAdd(Tabla t,int cuanto){
			if(this.ContainsKey(t)){
				this[t]+=cuanto;
			}else{
				this.Add(t,cuanto);
			}
			return this;
		}
		public ConjuntoTablas Add(Tabla t){
			return AddAdd(t,1);
		}
		public ConjuntoTablas AddRange(ConjuntoTablas conj){
			foreach(System.Collections.Generic.KeyValuePair<Tabla, int> t in conj){
				this.AddAdd(t.Key,t.Value);
			}
			return this;
		}
		public bool Contains(Tabla t){
			return ContainsKey(t);
		}
	}
	*/
	public class ListaSqlizable<TSqlizable>:Lista<TSqlizable> where TSqlizable : Sqlizable{
		public ListaSqlizable(){}
		public ListaSqlizable(params TSqlizable[] elementos){
			foreach(TSqlizable s in elementos){
				this.Add(s);
			}
		}
		public ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(TSqlizable s in this){
				rta.AddRange(s.Tablas(queTablas));
			}
			return rta;
		}
	}
	public abstract class Sentencia{
		ListaSqlizable<Sqlizable> ParteWhere=new ListaSqlizable<Sqlizable>();
		ListaSqlizable<Sqlizable> ParteHaving=new ListaSqlizable<Sqlizable>();
		public ConjuntoTablas TablasLibres=new ConjuntoTablas();
		protected System.Collections.Generic.Dictionary<string, string> AliasTablas=new System.Collections.Generic.Dictionary<string, string>();
		protected bool ForzarGroupBy=false;
		protected virtual void ClonarMiembros(Sentencia s){
			ParteWhere=new ListaSqlizable<Sqlizable>();
			ParteWhere.AddRange(s.ParteWhere);
			ParteHaving=new ListaSqlizable<Sqlizable>();
			ParteHaving.AddRange(s.ParteHaving);
			AliasTablas=new System.Collections.Generic.Dictionary<string, string>(s.AliasTablas);
		}
		public abstract ListaSqlizable<Sqlizable> Partes();
		public abstract ConjuntoTablas Tablas(QueTablas queTablas);
		Sentencia AgregarParte(ListaSqlizable<Sqlizable> ListaPartes,params ExpresionSql[]expresiones){
			foreach(ExpresionSql expresion in expresiones){
				if(ListaPartes.Count>0){
					ListaPartes.Add(new LiteralSql("\n AND "));
				}
				ListaPartes.Add(expresion);
			}
			return this;
		}
		public Sentencia Where(params ExpresionSql[] expresiones){
			return AgregarParte(ParteWhere,expresiones);
		}
		public Sentencia Having(params ExpresionSql[] expresiones){
			return AgregarParte(ParteHaving,expresiones);
		}
		public Sentencia GroupBy(){
			ForzarGroupBy=true;
			return this;
		}
		public ListaSqlizable<Sqlizable> DevolverParte(string clausula,ListaSqlizable<Sqlizable> ListaPartes){
			ListaSqlizable<Sqlizable> rta=new ListaSqlizable<Sqlizable>();
			if(ListaPartes.Count>0){
				rta.Add(new LiteralSql("\n "+clausula+" "));
				rta.AddRange(ListaPartes);
			}
			return rta;
		}
		public ListaSqlizable<Sqlizable> PartesWhere(){
			return DevolverParte("WHERE",ParteWhere);
		}
		public ListaSqlizable<Sqlizable> PartesHaving(){
			return DevolverParte("HAVING",ParteHaving);
		}
		public void AsignarAlias(Tabla t){
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
		public void AsignarAlias(){
			AliasTablas=new System.Collections.Generic.Dictionary<string, string>();
			foreach(Tabla t in Tablas(QueTablas.Aliasables).Keys){
				AsignarAlias(t);
			}
		}
		/*
		protected void RegistrarTabla(Tabla t){
			if(t!=null){
				if(TablasUsadas.IndexOf(t)<0){
					TablasUsadas.Add(t);
					AsignarAlias(t);
				}
			}
		}
		*/
		public virtual Sentencia Clonate(){
			Sentencia rta=(Sentencia) this.MemberwiseClone();
			rta.ClonarMiembros(this);
			return rta;
		}
		public Sentencia Libre(Tabla t){
			TablasLibres.Add(t);
			return this;
		}
		public Sentencia Libres(ConjuntoTablas ts){
			TablasLibres.AddRange(ts);
			return this;
		}
	}
	public class SelectInterno:Sqlizable{
		Sentencia SentenciaInterna;
		public SelectInterno(Sentencia SentenciaInterna){
			this.SentenciaInterna=SentenciaInterna;
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			if(queTablas==QueTablas.AlFrom){
				return new ConjuntoTablas();
			}else{
				return SentenciaInterna.Tablas(queTablas);
			}
		}
		public override string ToSql(BaseDatos db){
			return new Ejecutador(db).Dump(SentenciaInterna,true);
		}
		public override bool CandidatoAGroupBy {
			get { return false; }
		}
	}
	public class SentenciaUpdate:Sentencia{
		public Tabla TablaBase;
		public ListaSqlizable<Sqlizable> ParteSet=new ListaSqlizable<Sqlizable>();
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
				return ((db.UpdateConJoin)?CampoAsignado.ToSql(db):CampoAsignado.NombreCampo)
					+"="+ValorAsignar.ToSql(db);
			}
			public override bool CandidatoAGroupBy{ 
				get{
					Assert.Fail("No debería preguntar si un Set tiene variables");
					return false;
				}
			}
			public override ConjuntoTablas Tablas(QueTablas queTablas){
				return ValorAsignar.Tablas(queTablas);
			}
		}
		public override ListaSqlizable<Sqlizable> Partes(){
			ListaSqlizable<Sqlizable> todas=new ListaSqlizable<Sqlizable>();
			todas.AddRange(ParteSet);
			todas.AddRange(PartesWhere());
			return todas;
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas TablasUsadas=new ConjuntoTablas();
			TablasUsadas.Add(TablaBase);
			TablasUsadas.AddRange(ParteSet.Tablas(queTablas));
			TablasUsadas.AddRange(PartesWhere().Tablas(queTablas));
			TablasUsadas.AddRange(PartesHaving().Tablas(queTablas));
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
		public void AgregarEn(ListaSqlizable<Sqlizable> Partes,params Sqlizable[] Parte){
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
		public ListaSqlizable<Campo> Campos=new ListaSqlizable<Campo>();
		public SentenciaSelect(params Campo[] Campos){
			this.Campos.AddRange(Campos);
		}
		public override ListaSqlizable<Sqlizable> Partes(){
			ListaSqlizable<Sqlizable> todas=new ListaSqlizable<Sqlizable>();
			ListaSqlizable<Sqlizable> groupBy=new ListaSqlizable<Sqlizable>();
			bool tieneAgrupados=ForzarGroupBy;
			todas.Add(new LiteralSql("SELECT "));
			{
				ParteSeparadora coma=new ParteSeparadora(", ");
				ParteSeparadora sepGB=new ParteSeparadora("\n GROUP BY ",", ");
				foreach(Campo c in Campos){
					if(c is CampoAlias){
						ExpresionSql expresion=(c as CampoAlias).ExpresionBase;
						if(expresion.TipoAgrupada){
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
				foreach(Tabla t in Tablas(QueTablas.AlFrom).Keys){
					if(!TablasLibres.Contains(t)){
						coma.AgregarEn(todas,t);
					}
				}
			}
			todas.AddRange(PartesWhere());
			if(tieneAgrupados){
				todas.AddRange(groupBy);
				todas.AddRange(PartesHaving());
			}
			return todas;
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			rta.AddRange(Campos.Tablas(queTablas));
			rta.AddRange(PartesWhere().Tablas(queTablas));
			return rta;
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
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			return CampoSinAlias.Tablas(queTablas);
		}
	}
	public class SentenciaInsert:SentenciaSelect{
		Tabla TablaBase;
		enum ConValuesOSelect {ConValues, ConSelect};
		ConValuesOSelect conQue;
		public SentenciaInsert(Tabla TablaBase){
			this.TablaBase=TablaBase;
		}
		public SentenciaInsert Select(params Campable[] CamposOTablas){
			conQue=ConValuesOSelect.ConSelect;
			foreach(Campable cc in CamposOTablas){
				foreach(Campo c in cc.Campos()){
					if(TablaBase.TieneElCampo(c) && Campos.FindIndex(delegate(Campo campo){return campo.NombreCampo==c.NombreCampo;})<0){
						Campos.Add(c);
					}
				}
			}
			return this;
		}
		public Sentencia Valores(params Campable[] CamposConValores){
			conQue=ConValuesOSelect.ConValues;
			foreach(Campable cc in CamposConValores){
				foreach(Campo c in cc.Campos()){
					if(TablaBase.TieneElCampo(c) && Campos.FindIndex(delegate(Campo campo){return campo.NombreCampo==c.NombreCampo;})<0){
						Campos.Add(c);
					}
				}
			}
			return this;
		}
		public override ListaSqlizable<Sqlizable> Partes(){
			ListaSqlizable<Sqlizable> todas=new ListaSqlizable<Sqlizable>();
			ListaSqlizable<Sqlizable> valores=new ListaSqlizable<Sqlizable>();
			todas.Add(new LiteralSql("INSERT INTO "));
			todas.Add(TablaBase);
			TablaBase.Alias=null;
			ParteSeparadora coma=new ParteSeparadora(" (",", ");
			ParteSeparadora vcoma=new ParteSeparadora("VALUES (",", ");
			foreach(Campo c in Campos){
				coma.AgregarEn(todas,new CampoReceptorInsert(c));
				if(conQue==ConValuesOSelect.ConValues){
					if(c is CampoAlias){
						vcoma.AgregarEn(valores,(c as CampoAlias).ExpresionBase);
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
		ListaSqlizable<Campo> CamposContexto=new ListaSqlizable<Campo>();
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
			return Dump(laSentencia,false);
		}
		public string Dump(Sentencia laSentencia,bool interno){
			Sentencia s=laSentencia.Clonate();
			StringBuilder rta=new StringBuilder();
			if(!interno){
				s.AsignarAlias();
			}
			if(s is SentenciaUpdate){
				rta.Append("UPDATE ");
				SentenciaUpdate su=s as SentenciaUpdate;
				ConjuntoTablas suTablas=su.Tablas(QueTablas.AlFrom);
				if((suTablas.Count<=1 || !db.UpdateConJoin) && db.UpdateSoloUnaTabla){
					su.TablaBase.Alias=null;
				}
				rta.Append(su.TablaBase.ToSql(db));
				string prefijoSet="";
				string sufijoSet="";
				if(db.UpdateConJoin){
					foreach(Tabla t in su.Tablas(QueTablas.AlFrom).Keys){
						if(t!=su.TablaBase){
							if(t.TablaRelacionada!=null && su.Tablas(QueTablas.AlFrom).Contains(t.TablaRelacionada)){
								rta.Append(" INNER JOIN "+t.ToSql(db)+" ON ");
								Separador and=new Separador(" AND ");
								foreach(Campo c in t.CamposPk()){
									rta.Append(and+t.CamposRelacionadosFk[c].UnicoCampo().Igual(c).ToSql(db));
								}
								rta.Append("\n");
							}
						}
					}
					foreach(Tabla t in s.Tablas(QueTablas.AlFrom).Keys){
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
					foreach(Tabla t in su.Tablas(QueTablas.AlFrom).Keys){
						if(t!=su.TablaBase){
							if(t.TablaRelacionada!=null && su.Tablas(QueTablas.AlFrom).Contains(t.TablaRelacionada)){
								if(!s.TablasLibres.Contains(t)){
									parteFrom.Append(coma+t.ToSql(db));
								}
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
					rta.Append(setComa+(db.UpdateConJoin?p.CampoAsignado.ToSql(db):p.CampoAsignado.NombreCampo)+"="+prefijoSet+p.ValorAsignar.ToSql(db)+sufijoSet);
				}
				foreach(Sqlizable p in su.PartesWhere()){
					rta.Append(p.ToSql(db));
				}
			}else{
				if(s is SentenciaSelect && interno && db.InternosForzarAs){
					SentenciaSelect ss=s as SentenciaSelect;
					ListaSqlizable<Campo> nuevaLista=new ListaSqlizable<Campo>();
					foreach(Campo c in ss.Campos){
						if(c is CampoAlias){
							nuevaLista.Add(c);
						}else{
							nuevaLista.Add(c.Es(c));
						}
					}
					ss.Campos=nuevaLista;
				}
				foreach(Tabla t in s.Tablas(QueTablas.AlFrom).Keys){
					if(t.TablaRelacionada!=null && s.Tablas(QueTablas.AlFrom).Contains(t.TablaRelacionada)){
						foreach(Campo c in t.CamposPk()){
							s.Where(c.Igual(t.CamposRelacionadosFk[c]));
						}
					}
				}
				foreach(Tabla t in s.Tablas(QueTablas.AlFrom).Keys){
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
			if(!interno){
				rta.Append(";\n");
			}
			return rta.ToString();
		}
	}
	public class ExpresionSql:Sqlizable{
		public bool TipoAgrupada=false;
		bool candidatoAGroupBy=false;
		public override bool CandidatoAGroupBy{ get{ return candidatoAGroupBy; } }
		public ListaSqlizable<Sqlizable> Partes=new ListaSqlizable<Sqlizable>();
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
		public ExpresionSql(ListaSqlizable<Sqlizable> Partes){
			this.Partes=Partes;
			CalcularTipo();
		}
		public virtual ExpresionSql And(ExpresionSql otra){
			ListaSqlizable<Sqlizable> nueva=new ListaSqlizable<Sqlizable>();
			nueva.AddRange(Partes);
			nueva.Add(new LiteralSql("\n AND "));
			nueva.AddRange(otra.Partes);
			return new ExpresionSql(nueva);
		}
		public virtual ExpresionSql Or(ExpresionSql otra){
			ListaSqlizable<Sqlizable> nueva=new ListaSqlizable<Sqlizable>();
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
		public override ConjuntoTablas Tablas(QueTablas queTablas)
		{
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(Sqlizable s in Partes){
				rta.AddRange(s.Tablas(queTablas));
			}
			return rta;
		}
		public Campo UnicoCampo(){
			Assert.AreEqual(1,Partes.Count);
			return (Campo)Partes[0];
		}
		public ExpresionSql Operado<T>(string OperadorTextual,T expresion){
			if(OperadorTextual.Contains("(")){
				return new ExpresionSql(this,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion),new LiteralSql(")".PadRight(Cadena.CantidadOcurrencias('(',OperadorTextual),')')));
			}else{
				return new ExpresionSql(this,new LiteralSql(OperadorTextual),new ValorSql<T>(expresion));
			}
		}
		public ExpresionSql Dividido<T2>(T2 Valor){
			return Operado<T2>("/(",Valor);
		}
		public static ExpresionSql Agrupada<T>(string Operador, T expresion){
			ExpresionSql nueva=new ExpresionSql(new LiteralSql(Operador+"("),new ValorSql<T>(expresion),new LiteralSql(")"));
			nueva.TipoAgrupada=true;
			return nueva;
		}
		public static ExpresionSql Sum<T>(T expresion){
			return Agrupada("SUM",expresion);
		}
		public class SelectAgrupado:Sqlizable{
			Tabla TablaBase;
			ExpresionSql Expresion;
			string Operador;
			string PreOperador;
			string PostOperador;
			public Campo CampoSumar;
			// ExpresionSql ExpresionWhere;
			/*
			public SelectSuma(Tabla TablaBase,Campo CampoSumar,ExpresionSql ExpresionWhere){
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
			}
			*/
			public SelectAgrupado(Tabla TablaBase,Campo CampoSumar,ExpresionSql expresion,string Operador,string preOperador,string postOperador)
			{
				this.TablaBase=TablaBase;
				this.CampoSumar=CampoSumar;
				this.Expresion=expresion;
				this.Operador=Operador;
				this.PreOperador=preOperador;
				this.PostOperador=postOperador;
			}
			public override string ToSql(BaseDatos db)
			{
				// return "";
				StringBuilder rta=new StringBuilder();
				if(db.UpdateSelectSumViaDSum){
					Tabla TablaSumandis=CampoSumar.TablaContenedora;
					TablaSumandis.Alias=null;
					/*
					rta.Append(PreOperador+"D"+Operador+"("+PostOperador+"'"+db.StuffCampo(CampoSumar.NombreCampo)+"','"
					           +db.StuffTabla(TablaSumandis.NombreTabla));
					           */
					rta.Append(PreOperador+"D"+Operador+"("+
					           (PostOperador=="LOG("?db.FuncionLn+"(":PostOperador)
			                   +"'"+db.StuffCampo(CampoSumar.NombreCampo)+"','"
					           +TablaSumandis.ToSql(db));
					Separador and=new Separador("','"," AND ");
					if(Expresion!=null){
						rta.Append(and);
						foreach(Sqlizable p in Expresion.Partes){
							if(p is Campo){
								Campo c=p as Campo;
								if(c.TablaContenedora==TablaSumandis){
									rta.Append(p.ToSql(db));
								}else{
									if(c.TipoCampo.Contains("char")){
										rta.Append("'''"+p.ToSql(db)+"'''");
									}
								}
							}else{
								rta.Append(p.ToSql(db).Replace("'","''"));
							}
						}
					}
					if(PostOperador=="LOG("){
						rta.Append(and+CampoSumar.ToSql(db)+">0");
					}
					if(TablaSumandis==TablaBase.TablaRelacionada){
						foreach(Campo c in TablaBase.CamposPk()){
							string doblecomillas;
							if(c.TipoCampo.Contains("char")){
								doblecomillas="''";
							}else{
								doblecomillas="";
							}
							rta.Append(and+db.StuffCampo(TablaBase.CamposRelacionadosFk[c].UnicoCampo().NombreCampo)+"="+doblecomillas+"' & "+c.ToSql(db)+" & '"+doblecomillas);
						}
					}
					rta.Append("')".PadRight(Cadena.CantidadOcurrencias('(',PreOperador+PostOperador)+2,')'));
					return rta.ToString();
				}else{
					Tabla TablaSumandis=CampoSumar.TablaContenedora;
					rta.Append("(SELECT "+PreOperador+Operador+"("+PostOperador+CampoSumar.ToSql(db)+")".PadRight(Cadena.CantidadOcurrencias('(',PreOperador+PostOperador)+1,')')+
					           " FROM "
					           +TablaSumandis.ToSql(db));
					Separador and=new Separador(" WHERE "," AND ");
					if(PostOperador=="LOG("){
						rta.Append(and+CampoSumar.ToSql(db)+">0");
					}
					if(TablaSumandis==TablaBase.TablaRelacionada){
						foreach(Campo c in TablaBase.CamposPk()){
							rta.Append(and+TablaBase.CamposRelacionadosFk[c].ToSql(db)+"="+c.ToSql(db));
						}
					}
					rta.Append(")");
					return rta.ToString();
				}
			}
			public static implicit operator ExpresionSql(SelectAgrupado ss){
				return new ExpresionSql(ss);
			}
			public override bool CandidatoAGroupBy{ 
				get{
					return false;
				} 
			}
			public override ConjuntoTablas Tablas(QueTablas queTablas)
			{
				if(queTablas==QueTablas.AlFrom){
					return new ConjuntoTablas();
				}else{
					return new ConjuntoTablas(CampoSumar.TablaContenedora.Tablas(queTablas));
				}
			}
		}
	}
}
