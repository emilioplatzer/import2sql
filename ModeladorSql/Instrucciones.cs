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
	public class Sentencia:IElemento{
		protected ListaElementos<ElementoTipado<bool>> ClausulaWhere;
		public virtual string ToSql(BaseDatos db){
			Falla.Detener("No implementado aún");
			return null;
		}
		public virtual ConjuntoTablas Tablas(QueTablas queTablas){
			return new ConjuntoTablas();
		}
		/*
		public void AsignarAlias(){
			ConjuntoTablas TablasAlias=Tablas(QueTablas.Aliasables);
			foreach(TablasAlias
		}
		*/
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
		public ConjuntoTablas TablasQueEstanMasArriba;
		public SentenciaSelect(){
			ClausulaSelect=new ElementosClausulaSelect();
			ClausulaHaving=new ElementosClausulaHaving();
			TablasQueEstanMasArriba=new ConjuntoTablas();
		}
		public SentenciaSelect Select(params IConCampos[] campos){
			ClausulaSelect.AddRange(campos);
			return this;
		}
		public SentenciaSelect Having(params IElementoTipado<bool>[] campos){
			ClausulaHaving.AddRange(campos);
			return this;
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			Separador selectComa=new Separador("SELECT ",", ").AnchoLimitadoConIdentacion();
			StringBuilder groupby=new StringBuilder();
			Separador groupbyComa=new Separador("\n GROUP BY ",", ").AnchoLimitadoConIdentacion();
			bool TieneAgrupadas=false;
			foreach(IConCampos campos in ClausulaSelect){
				foreach(Campo c in campos.Campos()){
					selectComa.AgregarEn(rta,c.ToSql(db));
					IExpresion e=(c is ICampoAlias)?(c as ICampoAlias).ExpresionBase:c;
					TieneAgrupadas=TieneAgrupadas || e.EsAgrupada;
					if(e.CandidatoAGroupBy){
						groupbyComa.AgregarEn(groupby,e.ToSql(db));
					}
				}
			}
			Separador fromComa=new Separador("\n FROM ",", ").AnchoLimitadoConIdentacion();
			ConjuntoTablas TablasIncluidas=Tablas(QueTablas.AlFrom);
			foreach(Tabla t in TablasIncluidas.Keys){
				fromComa.AgregarEn(rta,t.ToSql(db));
			}
			Separador whereAnd=new Separador("\n WHERE ","\n AND ");
			foreach(Tabla t in TablasIncluidas.Keys){
				if(t.TablaRelacionada!=null){
					if(TablasIncluidas.Contiene(t.TablaRelacionada)){
						foreach(System.Collections.Generic.KeyValuePair<Campo, IExpresion> par in t.CamposRelacionFk){
							whereAnd.AgregarEn(rta,par.Key.ToSql(db)+"="+par.Value.ToSql(db));
						}
					}else if(!TablasQueEstanMasArriba.Contiene(t.TablaRelacionada)){
						Falla.Detener("Falta la tabla "+t.TablaRelacionada.NombreTabla+" relacionada a "+t.NombreTabla+" en:\n"+rta.ToString());
					}
				}
			}
			if(TieneAgrupadas){
				rta.Append(groupby);
			}
			Separador havingComa=new Separador("\n HAVING ",", ").AnchoLimitadoConIdentacion();
			foreach(IExpresion c in ClausulaHaving){
				IExpresion e=(c is ICampoAlias)?(c as ICampoAlias).ExpresionBase:c;
				havingComa.AgregarEn(rta,e.ToSql(db));
			}
			return rta.ToString();
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas)
		{
			ConjuntoTablas rta=base.Tablas(queTablas);
			rta.AddRange(ClausulaSelect.Tablas(queTablas));
			rta.AddRange(ClausulaHaving.Tablas(queTablas));
			return rta;
		}
	}
	public class SentenciaInsert:Sentencia{
		Tabla TablaBase;
		SentenciaSelect SentenciaSelectBase;
		ListaElementos<IConCampos> ValoresDirectos;
		public SentenciaInsert(Tabla TablaBase){
			this.TablaBase=TablaBase;
		}
		public SentenciaInsert Select(params IConCampos[] campos){
			Falla.SiNoEsNulo(ValoresDirectos,"En una sentencia insert no se puede poner un Select despues de Valores");
			SentenciaSelectBase=new SentenciaSelect();
			SentenciaSelectBase.Select(campos);
			return this;
		}
		public SentenciaInsert Having(params IElementoTipado<bool>[] campos){
			SentenciaSelectBase.Having(campos);
			return this;
		}
		public SentenciaInsert Valores(params IConCampos[] campos){
			Falla.SiNoEsNulo(SentenciaSelectBase,"En una sentencia insert no se puede poner Valores despues de un Select");
			ValoresDirectos=new ListaElementos<IConCampos>();
			ValoresDirectos.AddRange(campos);
			return this;
		}
		public override string ToSql(BaseDatos db){
			StringBuilder rta=new StringBuilder();
			rta.Append("INSERT INTO ");
			rta.Append(db.StuffTabla(TablaBase.NombreTabla));
			Separador coma=new Separador(" (",", ").AnchoLimitadoConIdentacion();
			ElementosClausulaSelect nuevaClausula=new ElementosClausulaSelect();
			foreach(IConCampos e in SentenciaSelectBase.ClausulaSelect){
				foreach(Campo c in e.Campos()){
					if(TablaBase.ContieneMismoNombre(c)){
						nuevaClausula.Add(c);
						rta.Append(coma+db.StuffCampo(c.NombreCampo));
					}
				}
			}
			SentenciaSelectBase.ClausulaSelect=nuevaClausula;
			rta.Append(")\n ");
			SentenciaSelectBase.TablasQueEstanMasArriba=new ConjuntoTablas(TablaBase);
			rta.Append(SentenciaSelectBase.ToSql(db));
			return rta.ToString();
		}
		public override ConjuntoTablas Tablas(QueTablas queTablas)
		{
			return SentenciaSelectBase.Tablas(queTablas);
		}
	}
	public class SentenciaUpdate:Sentencia{
		// ListaElementos<IElementoReNombrable> CamposUpdate;
	}
}
