/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 01:20 p.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Text;

using Comunes;
using BasesDatos;

namespace ModeladorSql
{
	public delegate void ProcesamientoTabla(Tabla tabla);
	public delegate void ProcesamientoPar(System.Collections.Generic.KeyValuePair<Campo, IExpresion> par);
	public class Sentencia:IElemento{
		public Tabla TablaBase; // Tabla base del Update o del subSelect cuando es un Not Exists
		public ConjuntoTablas TablasQueEstanMasArriba;
		public ListaElementos<ElementoTipado<bool>> ClausulaWhere=new ListaElementos<ElementoTipado<bool>>();
		protected bool IncluirJoinEnWhere=true;
		public void ParaCadaJunta(ConjuntoTablas tablas,Tabla TablaBase,ProcesamientoTabla procesarTabla,ProcesamientoPar procesarPar){
			var TablasVistas=new ConjuntoTablas();
			var TablasARevisar=new ConjuntoTablas();
			TablasARevisar.AddRange(tablas);
			if(TablaBase!=null){
				TablasVistas.Add(TablaBase);
				TablasARevisar.Remove(TablaBase);
			}
			var TablasNoIncluidas=new ConjuntoTablas();
			while(true){
				int CantidadARevisar=TablasARevisar.Count;
				foreach(Tabla t in TablasARevisar.Keys){
					if(t.TablaRelacionada!=null && (tablas.Contiene(t.TablaRelacionada) || TablasQueEstanMasArriba.Contiene(t.TablaRelacionada))){
						if(TablasVistas.Contiene(t.TablaRelacionada) || TablasQueEstanMasArriba.Contiene(t.TablaRelacionada)){
							procesarTabla(t);
							foreach(System.Collections.Generic.KeyValuePair<Campo, IExpresion> par in t.CamposRelacionFk){
								procesarPar(par);
							}
							TablasVistas.Add(t);
						}else{
							TablasNoIncluidas.Add(t);
							if((TablasQueEstanMasArriba==null || !TablasQueEstanMasArriba.Contiene(t.TablaRelacionada)) && !tablas.Contiene(t.TablaRelacionada)){
								Falla.Detener("Falta la tabla "+t.TablaRelacionada.NombreTabla+" relacionada a "+t.NombreTabla);
							}
						}
					}else{
						TablasVistas.Add(t);
					}
				}
				if(TablasNoIncluidas.Count==0){
			break;
				}else if(TablasNoIncluidas.Count==CantidadARevisar){
					if(TablasNoIncluidas.Count==1 && TablaBase==null){
			break; // es la única tabla no unida. 
					}
					Falla.Detener("FALLA AL ORDENAR EL JOIN "+TablasNoIncluidas.Count+"="+CantidadARevisar+": "+TablasNoIncluidas.ToString());
				}else{
					TablasARevisar=TablasNoIncluidas;
					TablasNoIncluidas=new ConjuntoTablas();
				}
			}
		}
		public virtual string ToSql(BaseDatos db){
			var rta=new StringBuilder();
			var whereAnd=new Separador("\n WHERE ","\n AND ");
			foreach(var e in ClausulaWhere){
				whereAnd.AgregarEn(rta,e.ToSql(db)/*.Replace(" AND ","\n AND ")*/);
			}
			var TablasAJoinear=Tablas(QueTablas.AlFrom);
			if(IncluirJoinEnWhere){
				ParaCadaJunta(TablasAJoinear, TablaBase, tabla => {},
					par => whereAnd.AgregarEn(rta,par.Key.ToSql(db)+"="+par.Value.ToSql(db))
				);
			}
			var TablasAContextualizar=Tablas(QueTablas.AlFrom);
			foreach(Tabla t in TablasAContextualizar.Keys){
				if(t.CamposContexto!=null 
				   /*&& (t.TablaRelacionada==null
				       || !TablasIncluidas.Contiene(t.TablaRelacionada))*/)
				{
					foreach(Campo c in t.CamposContexto){
						if(t.ContieneMismoNombre(c)){
							whereAnd.AgregarEn(rta,t.CampoIndirecto(c).ToSql(db)+"="+db.StuffValor(c.ValorSinTipo));
						}
					}
				}
			}
			return rta.ToString();
		}
		public virtual ConjuntoTablas Tablas(QueTablas queTablas){
			return ClausulaWhere.Tablas(queTablas);
		}
		/*
		public void AsignarAlias(){
			ConjuntoTablas TablasAlias=Tablas(QueTablas.Aliasables);
			foreach(TablasAlias
		}
		*/
	}
	public static class ParaSentencias{
		public static TSentencia Where<TSentencia> (this TSentencia s, params ElementoTipado<bool>[] ExpresionesWhere) where TSentencia:Sentencia {
			s.ClausulaWhere.AddRange(ExpresionesWhere);
			return s;
		}
		public static TSentenciaSelect Select<TSentenciaSelect>(this TSentenciaSelect s, params IConCampos[] campos) where TSentenciaSelect:SentenciaSelect{
			s.ClausulaSelect.AddRange(campos);
			return s;
		}
		public static TSentenciaSelect GroupBy<TSentenciaSelect>(this TSentenciaSelect s/*, params IConCampos[] campos*/) where TSentenciaSelect:SentenciaSelect{
			// s.ClausulaSelect.AddRange(campos);
			s.ConGroupBy=true;
			return s;
		}
		public static TSentenciaSelect Having<TSentenciaSelect>(this TSentenciaSelect s,params IElementoTipado<bool>[] campos) where TSentenciaSelect:SentenciaSelect{
			s.ClausulaHaving.AddRange(campos);
			return s;
		}
	}
	public class ElementosClausulaSelect:ListaElementos<IConCampos>{}
	public class ElementosClausula:ListaElementos<IElementoTipado<bool>>,IElemento{
		public string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador and=new Separador("\n AND ");
			foreach(IElementoTipado<bool> e in this){
				rta.Append(and+e.ToSql(db));
			}
			return rta.ToString();
		}
		/*
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			ConjuntoTablas rta=new ConjuntoTablas();
			foreach(IElementoTipado<bool> e in this){
				rta.AddRange(e.Tablas(queTablas));
			}
			return rta;
		}
		*/
	}
	public class ElementosClausulaWhere:ElementosClausula{}
	public class ElementosClausulaHaving:ElementosClausula{}
	public class SentenciaSelect:Sentencia{
		public ElementosClausulaSelect ClausulaSelect;
		public ElementosClausulaHaving ClausulaHaving;
		public ListaCampos ListaOrderBy=new ListaCampos();
		public bool ConGroupBy;
		public bool EsInterno;
		public bool EsVistaExistente;
		public Tabla TablaParaRestringirCampos;
		public SentenciaSelect(){
			ClausulaSelect=new ElementosClausulaSelect();
			ClausulaHaving=new ElementosClausulaHaving();
			TablasQueEstanMasArriba=new ConjuntoTablas();
		}
		public SentenciaSelect(params IConCampos[] campos)
			:this()
		{
			ClausulaSelect.AddRange(campos);
		}
		public SentenciaSelect OrderBy(params Campo[] campos){
			ListaOrderBy.AddRange(campos);
			return this;
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador selectComa=new Separador("SELECT ",", ").AnchoLimitadoConIdentacion();
			StringBuilder groupby=new StringBuilder();
			Separador groupbyComa=new Separador("\n GROUP BY ",", ").AnchoLimitadoConIdentacion();
			bool TieneAgrupadas=ConGroupBy;
			var CamposElegidos=new ListaCampos();
			foreach(IConCampos campos in ClausulaSelect){
				foreach(Campo c in campos.Campos()){
					if(TablaParaRestringirCampos==null || TablaParaRestringirCampos.ContieneMismoNombre(c) && !CamposElegidos.Exists(campo => c.NombreCampo==campo.NombreCampo)){
						CamposElegidos.Add(c);
						selectComa.AgregarEn(rta,c.ToSql(db,EsInterno && db.InternosForzarAs));
						// IExpresion e=(c is ICampoAlias)?(c as ICampoAlias).ExpresionBase:c;
						IExpresion e=c.Expresion;
						TieneAgrupadas=TieneAgrupadas || e.EsAgrupada;
						if(e.CandidatoAGroupBy){
							groupbyComa.AgregarEn(groupby,e.ToSql(db));
						}
					}
				}
			}
			Separador fromComa=new Separador("\n FROM ",", ").AnchoLimitadoConIdentacion();
			ConjuntoTablas TablasIncluidas=Tablas(QueTablas.AlFrom);
			foreach(Tabla t in TablasIncluidas.Keys){
				fromComa.AgregarEn(rta,t.ToSql(db));
			}
			rta.Append(base.ToSql(db));
			if(TieneAgrupadas){
				rta.Append(groupby);
			}
			Separador havingComa=new Separador("\n HAVING ","\n AND ").AnchoLimitadoConIdentacion();
			foreach(IExpresion e in ClausulaHaving){
				havingComa.AgregarEn(rta,e.ToSql(db));
			}
			Separador orderByComa=new Separador("\n ORDER BY ",", ").AnchoLimitadoConIdentacion();
			foreach(Campo c in ListaOrderBy){
				orderByComa.AgregarEn(rta,c.ToSql(db)+c.DireccionOrderBy);
			}
			return rta.ToString();
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas)
		{
			ConjuntoTablas rta=new ConjuntoTablas();
			rta.AddRange(ClausulaSelect.Tablas(queTablas));
			rta.AddRange(base.Tablas(queTablas));
			rta.AddRange(ClausulaHaving.Tablas(queTablas));
			return rta;
		}
	}
	public class SentenciaInsert:SentenciaSelect{
		Tabla TablaReceptora;
		// SentenciaSelect SentenciaSelectBase;
		ElementosClausulaSelect ValoresDirectos;
		public SentenciaInsert(Tabla TablaReceptora){
			this.TablaReceptora=TablaReceptora;
		}
		public SentenciaInsert Valores(params IConCampos[] campos){
			Falla.Si(ClausulaSelect.Count>0,"En una sentencia insert no se puede poner Valores despues de un Select");
			if(ValoresDirectos==null){
				ValoresDirectos=new ElementosClausulaSelect();
			}
			ValoresDirectos.AddRange(campos);
			return this;
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			rta.Append("INSERT INTO ");
			rta.Append(db.StuffTabla(TablaReceptora.NombreTabla));
			Separador coma=new Separador(" (",", ").AnchoLimitadoConIdentacion();
			ElementosClausulaSelect nuevaListaSelect=new ElementosClausulaSelect();
			ListaElementos<IExpresion> nuevaListaValores=new ListaElementos<IExpresion>();
			foreach(IConCampos e in ValoresDirectos??ClausulaSelect){
				foreach(Campo c in e.Campos()){
					if(TablaReceptora.ContieneMismoNombre(c)){
						if(!nuevaListaSelect.Exists(delegate(IConCampos campo){ return c.Nombre==campo.Campos()[0].Nombre; })){
							nuevaListaSelect.Add(c); // Esto va para el Exists
							if(ValoresDirectos!=null){
								if(!(c is ICampoAlias) && c.ValorSinTipo!=null){
									nuevaListaValores.Add(new Constante<object>(c.ValorSinTipo));
									coma.AgregarEn(rta,db.StuffCampo(c.NombreCampo));
								}else if(c is ICampoAlias){
									nuevaListaValores.Add(c);
									coma.AgregarEn(rta,db.StuffCampo(c.NombreCampo));
								}
							}else{
								coma.AgregarEn(rta,db.StuffCampo(c.NombreCampo));
							}
						}
					}
				}
			}
			if(ValoresDirectos!=null){
				Falla.Si(ClausulaSelect.Count>0,"No se pueden poner Valores directos y Select en un insert (contra tabla "+TablaReceptora.NombreTabla+")");
				Separador valuesComa=new Separador(")\n VALUES (",", ").AnchoLimitadoConIdentacion();
				foreach(IExpresion e in nuevaListaValores){
					valuesComa.AgregarEn(rta,e.Expresion.ToSql(db));
				}
				rta.Append(")");
			}else{
				ClausulaSelect=nuevaListaSelect;
				rta.Append(")\n ");
				// TablasQueEstanMasArriba=new ConjuntoTablas(TablaReceptora);
				rta.Append(base.ToSql(db));
			}
			return rta.ToString();
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			if(ValoresDirectos==null){
				return base.Tablas(queTablas);
			}else{
				return new ConjuntoTablas();
			}
		}
	}
	public class SentenciaUpdate:Sentencia{
		ListaElementos<ICampoAlias> Asignaciones=new ListaElementos<ICampoAlias>();
		public SentenciaUpdate(Tabla TablaBase,params ICampoAlias[] Asignaciones){
			this.TablaBase=TablaBase;
			this.Asignaciones.AddRange(Asignaciones);
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas){
			var rta=new ConjuntoTablas();
			rta.Add(TablaBase);
			rta.AddRange(Asignaciones.Tablas(queTablas));
			rta.AddRange(base.Tablas(queTablas));
			return rta;
		}
		public override string ToSql(BaseDatos db){
			var rta=new StringBuilder();
			rta.Append("UPDATE "+TablaBase.ToSql(db));
			if(db.UpdateConJoin){
				IncluirJoinEnWhere=false;
				var tablas=Tablas(QueTablas.AlFrom);
				TablasQueEstanMasArriba=new ConjuntoTablas(TablaBase);
				var onAnd=new Separador(" ON "," AND ");
				ParaCadaJunta(tablas,TablaBase
				    , tabla => {rta.Append(" INNER JOIN "+tabla.ToSql(db)); onAnd.Reiniciar(); }
					, par => onAnd.AgregarEn(rta,par.Value.ToSql(db)+"="+par.Key.ToSql(db))
				);
			}
			var setComa=new Separador("\n SET ",", ").AnchoLimitadoConIdentacion();
			foreach(var a in Asignaciones){
				setComa.AgregarEn(rta,
					(db.UpdateConJoin?a.CampoReceptor.ToSql(db):db.StuffCampo(a.CampoReceptor.NombreCampo))
		            +"="+a.ExpresionBase.ToSql(db)
				);
			}
			if(!db.UpdateConJoin){
				IncluirJoinEnWhere=true;
				Separador fromComa=new Separador("\n FROM ",", ").AnchoLimitadoConIdentacion();
				ConjuntoTablas TablasIncluidas=Tablas(QueTablas.AlFrom);
				TablasIncluidas.Remove(TablaBase);
				foreach(Tabla t in TablasIncluidas.Keys){
					fromComa.AgregarEn(rta,t.ToSql(db));
				}
			}
			rta.Append(base.ToSql(db));
			return rta.ToString();
		}
	}
	public class SentenciaDelete:Sentencia{
		public SentenciaDelete(Tabla TablaBase){
			this.TablaBase=TablaBase;
		}
		public override string ToSql(BaseDatos db){
			var rta=new StringBuilder();
			rta.Append("DELETE FROM "+TablaBase.ToSql(db));
			rta.Append(base.ToSql(db));
			return rta.ToString();
		}
	}
}
